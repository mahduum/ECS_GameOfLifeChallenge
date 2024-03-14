# ECS_GameOfLifeChallenge

I made two executables which that are available in the repo in `Executables` folder, for 1_000_000 and 562_500 entities that on my machine gave respectively 30 FPS and 60 FPS. Since my machine is kind of a moderate monster with Ryzen-9 and 24 cores and NVIDIA Quadro, as dowloadable .zip here I attached the latter in case should it be tested on something less monstrous.


My solution is very simple and what mostly eats frames is rendering and presentation systems and depending on how many entities are visible on screen (I tried to maximize their amount on screen). This is the lion part of performance hit of this sample and it mostly demonstrates that EnitityGraphics works really well.

As far as the actual solution, there  are three systems:
1. Spawning and assigning to entities their place on grid (I haven't optimized it too much, although it could be done for example to extend spawning in time across few frames)
2. Change state detection system, which a) maps entities assigned grid index to their current state alive/dead, b) marks entities that should change state.
3. Switch state system, which based on marker performs some logic related to state alive/dead - in the sample it simply changes `y` value of the transform.

At the beginning I thought that reacreating array that maps to state every frame would be cumbersome but it is barely noticeable. I tried for comparison a solution where entities hold direct references to their neighbours but it was two times slower at least and there was not enough memory for 1M entitities graphics as some kind of a drawback.

For 1M entities in profiler systems 2 and 3 combined which run every frame remain between 0.1 and 0.2 ms.
For 562_500 entities it is respectively much less, something like 0.05-0.1.

I thought about other potential optimizations like to further divide the operations into grid sections, dividing entities into buckets by quantized location and processing those buckets independently in `IParallelFor` but in the end I am not sure had it done better job than parallel IEntityJob already being executed by chunks.