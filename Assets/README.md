### Problem
Runtime collider creation is slow as it rebuilds physics cache.

### Benchmarks
1. MeshCollider.Create 3500 verts == 25MS with burst
1. CompoundCollider.Create == 16MS with burst
    1. 854 PolygonCollider.CreateQuad == 1MS


### Proposed solution
* Voxels
   * CompoundCollider with few quads in range.   
   * Update when
      * env changes
      * player moves 50% of collider range 
* Models
   * MeshCollider compute once



### Questions
1. How much of MeshCollider.create cost is internal checks?
1. How much of MeshCollider.create cost is welding?


### References
_"I routinely do 30k+ verts in maybe 15ms or less (in a Burst job)"_   
_"It usually freezes because of synchronous Burst compilation on mesh collider creation, that part is not due to the performance of the internal checks."_   
