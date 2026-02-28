# Wii Sports Golf: Custom Map Development Framework (BrawlCrate Fork)

## 1. Project Context

We are forking BrawlCrate to add native support for editing Wii Sports (2006) Golf maps. The goal is to create a specialized editor within BrawlCrate that handles the three-way synchronization between visuals (BRRES), collision (KCL), and map logic (PMP).

## 2. File Format Specifications (Technical)

### A. PMP (Pack MaP) — Logic and Object Placement

The PMP file orchestrates the gameplay scene.

- **Magic:** `PMPF` (0x00)
- **Object Count:** UInt16 (0x10)
- **Object Data Offset:** UInt32 (0x40)
- **Object Entry Structure** (approx. 0x58 bytes):
  - **0x00:** Object Group ID (UInt16)
  - **0x02:** Object ID (UInt16) — *Note: identify "Cup" and "Tee" here.*
  - **0x08:** Position Vector (Single)
  - **0x14:** Scaling Vector (Single)
  - **0x20:** Transformation Matrix 3×3 (Single)
  - **0x48:** Object-Specific Parameters (Varies)

### B. KCL (Nintendo Collision) — Physics

The KCL triangle mesh dictates how the ball interacts with the ground.

**Material Flags (Hex Values):**


| Hex  | Surface       | Behavior                                            |
| ---- | ------------- | --------------------------------------------------- |
| 0x00 | Fairway       | Standard bounce/roll                                |
| 0x01 | Green         | Lowest friction, triggers putting precision physics |
| 0x02 | Rough         | High friction, reduces power for next shot          |
| 0x03 | Bunker        | Immediate stop, zero roll, requires Wedge           |
| 0x04 | Water Hazard  | Trigger penalty/respawn                             |
| 0x05 | Out of Bounds | Trigger penalty/respawn                             |


**Constraints:** Must implement a "Balanced Octree." If the octree is too dense or a cell is too complex, the engine will suffer the "30-second lag" glitch.

### C. BRRES (MDL0) — Visuals

Visual representation of the course.

**Sync Requirement:** Moving a PMP object (like the Cup) or a KCL vertex (like a hill) requires an identical translation in the MDL0 vertex data to prevent "visual-physics mismatch."

## 3. Immediate Development Priorities

1. **Parser Implementation:** Implement a PMP node type in `BrawlLib/SSBB/ResourceNodes` to expose PMP objects in the BrawlCrate tree view.
2. **Object Gizmos:** Map PMP Position Vectors (0x08) to 3D gizmos in the viewport.
3. **Dual-Edit Sync:** Create a "Sync Move" feature where translating the "Cup" PMP object also translates the corresponding vertices in the visual MDL0 mesh.
4. **KCL Vertex Editor:** Allow Y-axis translation of KCL vertices to create hills/slopes, ensuring normals (**n**) are recalculated for proper ball roll physics.

## 4. Development Workflow (Hot-Swapping)

To test maps without restarting Dolphin:

- **Environment:** Use Dolphin's "loose file" loading (*Right-click game → Properties → File System → Extract entire Disc*).
- **Path:** Edit the loose `.szs` file in `Stage/Golf/Golf_X.szs`.
- **Cycle:**
  1. Modify file in BrawlCrate and save.
  2. In Dolphin, exit the current hole to the course selection menu.
  3. Re-enter the hole. The engine unloads/reloads the SZS archive, reflecting changes immediately.

