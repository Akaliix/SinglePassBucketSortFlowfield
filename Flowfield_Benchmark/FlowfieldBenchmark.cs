using System.Diagnostics;

namespace Flowfield_Benchmark
{
    public static class FlowfieldBenchmark
    {
        static readonly (int dx, int dy)[] OrthogonalDirs = { (-1, 0), (0, 1), (1, 0), (0, -1) };
        static readonly (int dx, int dy)[] DiagonalDirs = { (-1, 1), (1, 1), (1, -1), (-1, -1) };

        const int ORTHOGONAL_COST = 2;
        const int DIAGONAL_COST = 3;
        const int ROTATION_DIFF = DIAGONAL_COST - ORTHOGONAL_COST;
        const int WALL_COST = 50;
        const int MOD_FOR_BUCKET = DIAGONAL_COST + WALL_COST;

        public struct Cell
        {
            public (int x, int y) Pos;
            public int Cost;
            public int BestCost;

            public Cell((int x, int y) pos, int cost = 0)
            {
                Pos = pos;
                Cost = cost;
                BestCost = int.MaxValue;
            }
        }

        public struct CellForQueue
        {
            public (int x, int y) Pos;
            public int Cost;
            public int BestCost;
            public bool InQueue;

            public CellForQueue((int x, int y) pos, int cost = 0)
            {
                Pos = pos;
                Cost = cost;
                BestCost = int.MaxValue;
                InQueue = false;
            }
        }

        static Cell[,] InitGrid((int w, int h) gridSize, List<(int x, int y)> wallData)
        {
            var grid = new Cell[gridSize.w, gridSize.h];
            for (int x = 0; x < gridSize.w; x++)
                for (int y = 0; y < gridSize.h; y++)
                    grid[x, y] = new Cell((x, y));

            foreach (var (x, y) in wallData)
                grid[x, y] = new Cell((x, y), WALL_COST);

            return grid;
        }

        static CellForQueue[,] InitGridQueue((int w, int h) gridSize, List<(int x, int y)> wallData)
        {
            var grid = new CellForQueue[gridSize.w, gridSize.h];
            for (int x = 0; x < gridSize.w; x++)
                for (int y = 0; y < gridSize.h; y++)
                    grid[x, y] = new CellForQueue((x, y));

            foreach (var (x, y) in wallData)
                grid[x, y] = new CellForQueue((x, y), WALL_COST);

            return grid;
        }

        static bool IsValid((int x, int y) pos, (int w, int h) gridSize)
            => pos.x >= 0 && pos.y >= 0 && pos.x < gridSize.w && pos.y < gridSize.h;

        public static (Cell[,], double, int, long) Dijkstra((int w, int h) gridSize, List<(int x, int y)> players, List<(int x, int y)> walls)
        {
            var grid = InitGrid(gridSize, walls);

            var sw = Stopwatch.StartNew();
            int maxHeapCount = 0;
            var heap = new PriorityQueue<(int x, int y, bool orthogonal), int>();

            foreach (var pos in players)
            {
                grid[pos.x, pos.y].BestCost = 0;
                heap.Enqueue((pos.x, pos.y, true), 0);
            }
            int loop_count = 0;
            while (heap.Count > 0)
            {
#if MEMORY
                if (heap.Count > maxHeapCount) maxHeapCount = heap.Count;
#endif

                var (x, y, is_orthogonal) = heap.Dequeue();
                var current = grid[x, y];
                loop_count++;

                if (is_orthogonal)
                {
                    heap.Enqueue((x, y, false), current.BestCost + ROTATION_DIFF);
                    foreach (var (dx, dy) in OrthogonalDirs)
                    {
                        var nx = x + dx;
                        var ny = y + dy;
                        if (!IsValid((nx, ny), gridSize)) continue;

                        var neighbor = grid[nx, ny];
                        int newCost = current.BestCost + neighbor.Cost + ORTHOGONAL_COST;
                        if (newCost < neighbor.BestCost)
                        {
                            neighbor.BestCost = newCost;
                            heap.Enqueue((nx, ny, true), newCost);
                            grid[nx, ny] = neighbor;
                        }
                    }
                }
                else
                {
                    foreach (var (dx, dy) in DiagonalDirs)
                    {
                        var nx = x + dx;
                        var ny = y + dy;
                        if (!IsValid((nx, ny), gridSize)) continue;
                        var neighbor = grid[nx, ny];
                        int newCost = current.BestCost + neighbor.Cost + DIAGONAL_COST;
                        if (newCost < neighbor.BestCost)
                        {
                            neighbor.BestCost = newCost;
                            heap.Enqueue((nx, ny, true), newCost);
                            grid[nx, ny] = neighbor;
                        }
                    }
                }
            }
            sw.Stop();

            long heapMemory = maxHeapCount * (sizeof(int) * 2 + sizeof(bool) + sizeof(int));
            return (grid, sw.Elapsed.TotalSeconds, loop_count, heapMemory);
        }

        public static (Cell[,], double, int, long) Bucket((int w, int h) gridSize, List<(int x, int y)> players, List<(int x, int y)> walls)
        {
            var grid = InitGrid(gridSize, walls);
            var sw = Stopwatch.StartNew();

            int maxBucketCount = 0;
            var buckets = new Queue<(int x, int y, bool orthogonal)>[MOD_FOR_BUCKET];

            for (int i = 0; i < MOD_FOR_BUCKET; i++)
                buckets[i] = new Queue<(int x, int y, bool orthogonal)>();

            foreach (var pos in players)
            {
                grid[pos.x, pos.y].BestCost = 0;
                buckets[0].Enqueue((pos.x, pos.y, true));
            }
            int loop_count = 0;
            int minCost = 0, empty = 0;
            while (empty < MOD_FOR_BUCKET)
            {
#if MEMORY
                int bcount = 0;
                foreach (var b in buckets)
                    bcount += b.Count;
                if (bcount > maxBucketCount) maxBucketCount = bcount;
#endif

                int index = minCost % MOD_FOR_BUCKET;
                var bucket = buckets[index];
                if (bucket.Count == 0)
                {
                    empty++;
                    minCost++;
                    continue;
                }
                loop_count++;
                empty = 0;
                var (x, y, is_orthogonal) = bucket.Dequeue();
                var current = grid[x, y];
                //if (current.BestCost != minCost) continue;
                if (is_orthogonal)
                {
                    buckets[(current.BestCost + ROTATION_DIFF) % MOD_FOR_BUCKET].Enqueue((x, y, false));
                    foreach (var (dx, dy) in OrthogonalDirs)
                    {
                        var nx = x + dx;
                        var ny = y + dy;
                        if (!IsValid((nx, ny), gridSize)) continue;

                        var neighbor = grid[nx, ny];
                        int newCost = current.BestCost + neighbor.Cost + ORTHOGONAL_COST;
                        if (newCost < neighbor.BestCost)
                        {
                            neighbor.BestCost = newCost;
                            int key = newCost % MOD_FOR_BUCKET;
                            buckets[key].Enqueue((nx, ny, true));
                            grid[nx, ny] = neighbor;
                        }
                    }
                }
                else
                {
                    foreach (var (dx, dy) in DiagonalDirs)
                    {
                        var nx = x + dx;
                        var ny = y + dy;
                        if (!IsValid((nx, ny), gridSize)) continue;

                        var neighbor = grid[nx, ny];
                        int newCost = current.BestCost + neighbor.Cost + DIAGONAL_COST;
                        if (newCost < neighbor.BestCost)
                        {
                            neighbor.BestCost = newCost;
                            int key = newCost % MOD_FOR_BUCKET;
                            buckets[key].Enqueue((nx, ny, true));
                            grid[nx, ny] = neighbor;
                        }
                    }
                }
            }
            sw.Stop();

            long bucketMemory = maxBucketCount * (sizeof(int) * 2 + sizeof(bool)) + sizeof(int) * MOD_FOR_BUCKET;
            return (grid, sw.Elapsed.TotalSeconds, loop_count, bucketMemory);
        }

        public static (CellForQueue[,], double, int, long) Queue((int w, int h) gridSize, List<(int x, int y)> players, List<(int x, int y)> walls)
        {
            var grid = InitGridQueue(gridSize, walls);
            var sw = Stopwatch.StartNew();
            int maxQueueCount = 0;
            var q = new Queue<(int x, int y)>();

            foreach (var pos in players)
            {
                grid[pos.x, pos.y].BestCost = 0;
                q.Enqueue((pos.x, pos.y));
            }
            int loop_count = 0;
            while (q.Count > 0)
            {
#if MEMORY
                if (q.Count > maxQueueCount) maxQueueCount = q.Count;
#endif
                var (x, y) = q.Dequeue();
                var current = grid[x, y];
                current.InQueue = false;
                grid[x, y] = current;
                loop_count++;

                foreach (var (dx, dy) in OrthogonalDirs)
                {
                    var nx = x + dx;
                    var ny = y + dy;
                    if (!IsValid((nx, ny), gridSize)) continue;

                    var neighbor = grid[nx, ny];
                    int newCost = current.BestCost + neighbor.Cost + ORTHOGONAL_COST;
                    if (newCost < neighbor.BestCost)
                    {
                        neighbor.BestCost = newCost;
                        if (!neighbor.InQueue)
                        {
                            neighbor.InQueue = true;
                            q.Enqueue((nx, ny));
                        }
                        grid[nx, ny] = neighbor;
                    }
                }

                foreach (var (dx, dy) in DiagonalDirs)
                {
                    var nx = x + dx;
                    var ny = y + dy;
                    if (!IsValid((nx, ny), gridSize)) continue;

                    var neighbor = grid[nx, ny];
                    int newCost = current.BestCost + neighbor.Cost + DIAGONAL_COST;
                    if (newCost < neighbor.BestCost)
                    {
                        neighbor.BestCost = newCost;
                        if (!neighbor.InQueue)
                        {
                            neighbor.InQueue = true;
                            q.Enqueue((nx, ny));
                        }
                        grid[nx, ny] = neighbor;
                    }
                }
            }
            sw.Stop();

            long queueMemory = maxQueueCount * (sizeof(int) * 2);
            return (grid, sw.Elapsed.TotalSeconds, loop_count, queueMemory);
        }


        // Remaining: GenerateConcentricWalls, Benchmark
        public static void BenchmarkFlowfield()
        {
            //WarmUp();
            var sizes = new List<(int width, int height)>
            {
                (64, 64),
                (128, 128),
                (256, 256),
                (512, 512),
                (1024, 1024),
                (2048, 2048),
                (4096, 4096),
                //(8192, 8192),
            };
            int numTrials = 1;

            string filePath = "flowfield_memory_results.txt";

            foreach (var item in sizes)
            {
                (int w, int h) gridSize = item;
                List<(int x, int y)> players = new() { ((int)(gridSize.w / 2.0), (int)(gridSize.h / 2.0)) };//GenerateRandomPlayers(gridSize);
                List<(int x, int y)> walls = new List<(int x, int y)>();//GenerateConcentricWallsWithoutCorners(gridSize, 2, players);//GenerateRandomWalls(gridSize, 0.5f);

                Console.WriteLine($"\n🔍 Benchmarking flowfields for grid size: {gridSize.w}x{gridSize.h}");
                var bucketTimes = new List<double>();
                var dijkstraTimes = new List<double>();
                var queueTimes = new List<double>();
                int totalCells = gridSize.w * gridSize.h;
                long memory1 = 0, memory2 = 0, memory3 = 0;
                for (int trial = 0; trial < numTrials; trial++)
                {
                    Console.WriteLine($"\nTrial {trial + 1}/{numTrials}");

                    var (grid1, time1, _, memoryt1) = Dijkstra(gridSize, players, walls);
                    memory1 = memoryt1;
                    dijkstraTimes.Add(time1);
                    Console.WriteLine($"Dijkstra:   {time1:F6} sec");

                    var (grid2, time2, _, memoryt2) = Bucket(gridSize, players, walls);
                    memory2 = memoryt2;
                    bucketTimes.Add(time2);
                    Console.WriteLine($"Bucket:     {time2:F6} sec");

                    var (grid3, time3, _, memoryt3) = Queue(gridSize, players, walls);
                    memory3 = memoryt3;
                    queueTimes.Add(time3);
                    Console.WriteLine($"Queue:      {time3:F6} sec");
                }

                double avgBucket = bucketTimes.Average();
                double avgDijkstra = dijkstraTimes.Average();
                double avgQueue = queueTimes.Average();

                Console.WriteLine($"\nBucket (avg): {avgBucket:F4} sec ({avgBucket / totalCells * 1e6:F3} µs/cell)");
                Console.WriteLine($"Dijkstra (avg): {avgDijkstra:F4} sec ({avgDijkstra / totalCells * 1e6:F3} µs/cell)");
                Console.WriteLine($"Queue (avg):    {avgQueue:F4} sec ({avgQueue / totalCells * 1e6:F3} µs/cell)");

                File.AppendAllText(filePath,
                    $"Grid Size: {gridSize.w}x{gridSize.h}\n" +
                    $"Bucket (avg):     {avgBucket:F4} sec ({avgBucket / totalCells * 1e6:F3} µs/cell) | {memory1 / 1024.0:F3} KB ({memory1 / (float)totalCells:F3} byte/cell)\n" +
                    $"Dijkstra (avg):   {avgDijkstra:F4} sec ({avgDijkstra / totalCells * 1e6:F3} µs/cell) | {memory2 / 1024.0:F3} KB ({memory2 / (float)totalCells:F3} byte/cell)\n" +
                    $"Queue (avg):      {avgQueue:F4} sec ({avgQueue / totalCells * 1e6:F3} µs/cell) | {memory3 / 1024.0:F3} KB ({memory3 / (float)totalCells:F3} byte/cell)\n\n");
            }
        }

        public static void WarmUp()
        {
            var gridSize = (4096, 4096);
            List<(int x, int y)> players = GenerateRandomPlayers(gridSize);
            List<(int x, int y)> walls = GenerateRandomWalls(gridSize, 0.5f);
            var (_, time, lcount, _) = Dijkstra(gridSize, players, walls);
            Console.WriteLine($"Dijkstra: {time:F6} sec ({lcount} loops)");
            var (_, time2, lcount2, _) = Bucket(gridSize, players, walls);
            Console.WriteLine($"Bucket:   {time2:F6} sec ({lcount2} loops)");
            var (_, time3, lcount3, _) = Queue(gridSize, players, walls);
            Console.WriteLine($"Queue:    {time3:F6} sec ({lcount3} loops)");
        }


        //public static void BenchmarkFlowfield()
        //{
        //    (int w, int h) gridSize = (999, 999);
        //    List<(int x, int y)> players = new() { ((int)(gridSize.w / 2.0), (int)(gridSize.h / 2.0)) };
        //    List<(int x, int y)> walls = GenerateConcentricWallsWithoutCorners(gridSize, 2, players);

        //    Console.WriteLine("Benchmarking flowfield pathfinding methods...\n");

        //    var (grid1, time1, lcount1) = Dijkstra(gridSize, players, walls);
        //    Console.WriteLine($"Dijkstra: {time1:F6} sec ({lcount1} loops)");

        //    var (grid2, time2, lcount2) = Bucket(gridSize, players, walls);
        //    Console.WriteLine($"Bucket:   {time2:F6} sec ({lcount2} loops)");
        //    //ExportBestCostGridToCsv(grid2, "test_bucket.csv");

        //    var (grid3, time3, lcount3) = Queue(gridSize, players, walls);
        //    Console.WriteLine($"Queue:    {time3:F6} sec ({lcount3} loops)");
        //    //ExportBestCostGridToCsv(grid3, "test_queue.csv");

        //    // Compare grid1 and grid2 if they are equal
        //    bool areEqual = true;
        //    for (int x = 0; x < gridSize.w; x++)
        //    {
        //        for (int y = 0; y < gridSize.h; y++)
        //        {
        //            if (grid1[x, y].BestCost != grid2[x, y].BestCost)
        //            {
        //                areEqual = false;
        //                break;
        //            }
        //        }
        //        if (!areEqual) break;
        //    }
        //    Console.WriteLine($"Dijkstra and Bucket results are equal: {areEqual}");

        //    // Compare grid2 and grid3 if they are equal
        //    areEqual = true;
        //    for (int x = 0; x < gridSize.w; x++)
        //    {
        //        for (int y = 0; y < gridSize.h; y++)
        //        {
        //            if (grid2[x, y].BestCost != grid3[x, y].BestCost)
        //            {
        //                areEqual = false;
        //                break;
        //            }
        //        }
        //        if (!areEqual) break;
        //    }
        //    Console.WriteLine($"Bucket and Queue results are equal: {areEqual}");
        //}

        public static List<(int x, int y)> GenerateConcentricWallsWithoutCorners((int w, int h) gridSize, int spacing, List<(int x, int y)> playerPositions)
        {
            List<(int x, int y)> wallPositions = new();
            int maxRings = Math.Min(gridSize.w, gridSize.h) / 2 / spacing;
            int cx = gridSize.w / 2;
            int cy = gridSize.h / 2;

            for (int ring = 1; ring <= maxRings; ring++)
            {
                int size = ring * spacing;
                for (int i = -size + 1; i <= size - 1; i++)
                {
                    int left = cx - size;
                    int right = cx + size;
                    int top = cy - size;
                    int bottom = cy + size;

                    if (IsValid((left, cy + i), gridSize)) wallPositions.Add((left, cy + i));
                    if (IsValid((right, cy + i), gridSize)) wallPositions.Add((right, cy + i));
                    if (IsValid((cx + i, top), gridSize)) wallPositions.Add((cx + i, top));
                    if (IsValid((cx + i, bottom), gridSize)) wallPositions.Add((cx + i, bottom));
                }
            }

            return wallPositions;
        }

        public static List<(int x, int y)> GenerateRandomWalls((int w, int h) gridSize, float percent)
        {
            List<(int x, int y)> wallPositions = new();
            Random rand = new();
            for (int x = 0; x < gridSize.w; x++)
            {
                for (int y = 0; y < gridSize.h; y++)
                {
                    if (rand.NextDouble() < percent)
                    {
                        wallPositions.Add((x, y));
                    }
                }
            }
            return wallPositions;
        }

        public static List<(int x, int y)> GenerateRandomPlayers((int w, int h) gridSize)
        {
            List<(int x, int y)> playerPositions = new();
            Random rand = new();
            for (int i = 0; i < rand.Next(2, 20); i++)
            {
                int x = rand.Next(0, gridSize.w);
                int y = rand.Next(0, gridSize.h);
                playerPositions.Add((x, y));
            }
            return playerPositions;
        }


        public static void ExportBestCostGridToCsv(Cell[,] grid, string filename)
        {
            using StreamWriter writer = new(filename);
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                string[] row = new string[width];
                for (int x = 0; x < width; x++)
                {
                    var cell = grid[x, y];
                    row[x] = cell.BestCost == int.MaxValue ? "inf" : cell.BestCost.ToString("F0");
                }
                writer.WriteLine(string.Join(",", row));
            }
        }

        public static void ExportBestCostGridToCsv(CellForQueue[,] grid, string filename)
        {
            using StreamWriter writer = new(filename);
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                string[] row = new string[width];
                for (int x = 0; x < width; x++)
                {
                    var cell = grid[x, y];
                    row[x] = cell.BestCost == int.MaxValue ? "inf" : cell.BestCost.ToString("F0");
                }
                writer.WriteLine(string.Join(",", row));
            }
        }

        public static void ExportCostGridToCsv(Cell[,] grid, string filename)
        {
            using StreamWriter writer = new(filename);
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                string[] row = new string[width];
                for (int x = 0; x < width; x++)
                {
                    var cell = grid[x, y];
                    //row[x] = cell.Cost > 0 ? "O" : "X";
                    row[x] = cell.Cost.ToString();
                }
                writer.WriteLine(string.Join(",", row));
            }
        }
    }
}
