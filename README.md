# Flowfield Benchmark: Comparing Pathfinding Algorithms in C# 


## Overview 


This project benchmarks three flow field pathfinding algorithms implemented in C#:

 
- **Dijkstra-Based Flow Field (Priority Queue)**
 
- **Bucket-Based Flow Field (Modular Queue)**
 
- **Queue-Based Flow Field (Simple FIFO Queue)**


The goal is to evaluate the computational efficiency, memory usage, and scalability of each method across a range of grid sizes and obstacle densities. All implementations support 8-directional movement and variable terrain costs.



---



## Features 

 
- ✅ Benchmarks on grids from `128x128` up to `4096x4096`
 
- ✅ Performance evaluation in 3 distinct scenarios:

 
  - No Obstacles
 
  - Concentric Wall Rings
 
  - 50% Random Wall Density
 
- ✅ Metrics:

 
  - Per-cell execution time (μs/cell)
 
  - Per-cell memory usage (B/cell)
 
- ✅ Single and multi-agent (player) support
 
- ✅ Modular code with separate grid initialization for each algorithm



---



## Algorithms

### 🟦 Dijkstra-Based Flow Field

- Uses a priority queue to select cells with the lowest cumulative cost.

- Processes each cell once due to sorting.

- Performs well in random terrain, but uses more memory and time than bucket-based methods, making it less ideal.

- Can handle float-based costs directly, as the priority queue supports precise sorting of floating-point numbers.

### 🟩 Bucket-Based Flow Field

- Uses a fixed-size modular queue indexed by cost.

- Processes each cell once due to sorting.

- Highly efficient in both dense and sparse environments.

- **Best overall performance**.

- Requires costs to be discretized into fixed intervals (e.g., integers or floats with a fixed step like `0.2f`) to group cells into buckets for efficient processing.

### 🟨 Queue-Based Flow Field

- FIFO queue with in-queue tracking.

- May process each cell more than once due to lack of sorting.

- Fast and memory-light in open fields or sparse environments.

- Simple and easy to implement.

---



## Example Usage 


You can run benchmarks via static calls like:



```csharp
var (grid, time, loop, mem) = FlowfieldBenchmark.Dijkstra(gridSize, players, walls);
```


Each function returns:

 
- The processed grid
 
- Total time in microseconds

- Loop count (processed nodes)

- Max queue size (if tracked)

---



## Methodology 


### Movement Costs 

 
- Orthogonal (N/S/E/W): `2 to 5 units`
 
- Diagonal (NW/NE/SW/SE): `3 to 7 units`
 
- Obstacles: `50 units` (treated as high-cost terrain)


### Grid Types 

 
- **No Walls** : Empty, open map
 
- **Concentric Walls** : Square ring barriers with corner gaps
 
- **Random Walls** : 50% density, randomly distributed


### Evaluation 

 
- Execution time recorded using `Stopwatch`
 
- Memory usage estimated per grid cell
 
- No rendering or path reconstruction included



---



## Results Summary 

| Scenario | Best Performer | Notes | 
| --- | --- | --- | 
| No Walls | Queue-Based (FIFO) | Fastest and most memory-efficient in ideal maps | 
| Concentric Walls | Bucket-Based (Modular) | Up to 50× faster and 10× less memory than others | 
| 50% Random Walls | Bucket-Based | Balanced performance across large, noisy grids | 



### 📊 Performance & Memory Benchmarks 


#### Average Execution Time and Per-Cell Processing Time (No Walls) 

| Grid Size | Dijkstra (μs/cell) | Bucket (μs/cell) | Queue (μs/cell) | 
| --- | --- | --- | --- | 
| 128x128 | 0.124 | 0.052 | 0.029 | 
| 256x256 | 0.130 | 0.043 | 0.028 | 
| 512x512 | 0.144 | 0.046 | 0.032 | 
| 1024x1024 | 0.152 | 0.055 | 0.036 | 
| 2048x2048 | 0.167 | 0.065 | 0.063 | 
| 4096x4096 | 0.181 | 0.072 | 0.080 | 


#### Memory Usage and Per-Cell Memory (No Walls) 

| Grid Size | Dijkstra (B/cell) | Bucket (B/cell) | Queue (B/cell) | 
| --- | --- | --- | --- | 
| 128x128 | 0.468 | 0.383 | 0.247 | 
| 256x256 | 0.251 | 0.190 | 0.124 | 
| 512x512 | 0.120 | 0.094 | 0.062 | 
| 1024x1024 | 0.064 | 0.047 | 0.031 | 
| 2048x2048 | 0.031 | 0.023 | 0.016 | 
| 4096x4096 | 0.016 | 0.012 | 0.008 | 


#### Average Execution Time and Per-Cell Processing Time (Concentric Walls) 

| Grid Size | Dijkstra (μs/cell) | Bucket (μs/cell) | Queue (μs/cell) | 
| --- | --- | --- | --- | 
| 128x128 | 0.159 | 0.056 | 0.174 | 
| 256x256 | 0.162 | 0.045 | 0.326 | 
| 512x512 | 0.176 | 0.051 | 0.632 | 
| 1024x1024 | 0.190 | 0.060 | 1.248 | 
| 2048x2048 | 0.204 | 0.073 | 3.868 | 


#### Memory Usage and Per-Cell Memory (Concentric Walls) 

| Grid Size | Dijkstra (B/cell) | Bucket (B/cell) | Queue (B/cell) | 
| --- | --- | --- | --- | 
| 128x128 | 2.108 | 1.498 | 1.186 | 
| 256x256 | 1.098 | 0.782 | 1.093 | 
| 512x512 | 0.557 | 0.399 | 1.047 | 
| 1024x1024 | 0.282 | 0.202 | 1.023 | 
| 2048x2048 | 0.142 | 0.101 | 1.012 | 


#### Execution Time and Per-Cell Processing Time (50% Random Walls) 

| Grid Size | Dijkstra (μs/cell) | Bucket (μs/cell) | Queue (μs/cell) | 
| --- | --- | --- | --- | 
| 128x128 | 0.169 | 0.063 | 0.091 | 
| 256x256 | 0.185 | 0.056 | 0.095 | 
| 512x512 | 0.183 | 0.058 | 0.128 | 
| 1024x1024 | 0.201 | 0.069 | 0.190 | 
| 2048x2048 | 0.203 | 0.075 | 0.635 | 
| 4096x4096 | 0.249 | 0.114 | 1.482 | 


#### Per-Cell Memory Usage (50% Random Walls) 

| Grid Size | Dijkstra (B/cell) | Bucket (B/cell) | Queue (B/cell) | 
| --- | --- | --- | --- | 
| 128x128 | 3.279 | 2.307 | 0.531 | 
| 256x256 | 1.583 | 1.119 | 0.243 | 
| 512x512 | 0.736 | 0.519 | 0.194 | 
| 1024x1024 | 0.376 | 0.265 | 0.122 | 
| 2048x2048 | 0.225 | 0.159 | 0.155 | 
| 4096x4096 | 0.090 | 0.063 | 0.129 | 

