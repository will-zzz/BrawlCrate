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
  - **0x02:** Object ID (UInt16) — *Note: identify "Tee" (ID 0) and "Cup" (ID 1) here.*
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

**Sync Requirement:** Moving a PMP object (like the Cup/hole or Tee) or a KCL vertex (like a hill) requires an identical translation in the MDL0 vertex data to prevent "visual-physics mismatch."

## 3. Immediate Development Priorities

1. **Parser Implementation:** Implement a PMP node type in `BrawlLib/SSBB/ResourceNodes` to expose PMP objects in the BrawlCrate tree view.
2. **Object Gizmos:** Map PMP Position Vectors (0x08) to 3D gizmos in the viewport.
3. **Dual-Edit Sync:** Create a "Sync Move" feature where translating the "Cup" (hole) PMP object also translates the corresponding vertices in the visual MDL0 mesh.
4. **KCL Vertex Editor:** Allow Y-axis translation of KCL vertices to create hills/slopes, ensuring normals (**n**) are recalculated for proper ball roll physics.

## 4. Development Workflow (Hot-Swapping)

To test maps without restarting Dolphin:

- **Environment:** Use Dolphin's **loose file** loading so the game reads from an extracted folder instead of the disc image. That way when you save from BrawlCrate, Dolphin will use your edited files the next time it loads that content.
- **Path:** Course files live under the game’s filesystem, e.g. `files/Stage/RPGolScene/` (e.g. `gif_course_fc1.carc`, `glf_course_fc1.carc`). The exact path may vary by game/region.

### Step-by-step: Run edited courses in Dolphin

1. **Extract the game (one-time)**  
   - In Dolphin: right‑click **Wii Sports** → **Properties** → **File System** (or **Open Wii Save Folder** / **Browse** depending on Dolphin version).  
   - Use **Extract entire disc** (or **Extract entire NAND** if you’re using NAND) and choose a folder (e.g. `C:\Dolphin\Extracted\WiiSports`).  
   - Wait until extraction finishes. You should see a `files` (or similar) folder with `Stage`, `RPGolScene`, etc.

2. **Find the course file**  
   - In the extracted folder, go to e.g. `files/Stage/RPGolScene/`.  
   - You’ll see `.carc` files (e.g. `gif_course_fc1.carc`, `glf_course_fc1.carc`).  
   - Remember this path; you’ll save back here from BrawlCrate.

3. **Edit in BrawlCrate**  
   - In BrawlCrate: **File → Open** and open the `.carc` from the extracted path (e.g. `…/RPGolScene/glf_course_fc1.carc`).  
   - Make your changes (e.g. move a Cup’s Position for the hole, or a Tee’s for the start).  
   - **File → Save** (or **Save As**) and save **to the same path**, overwriting the original `.carc`.  
   - Close the file or leave it open; the important part is that the file on disk is updated.

4. **Run in Dolphin**  
   - Start Dolphin and run **Wii Sports** the way you normally do **from the extracted content** (or from a disc image that’s configured to use the same path—depends how you set up “load from extracted”).  
   - Go to Golf → pick the course and hole you edited.  
   - Play the hole. Your edits are in the file Dolphin is loading.

5. **See changes without restarting**  
   - After playing (or at any time), **exit the hole** back to the course selection (or main menu).  
   - **Re-enter the same hole** (or same course).  
   - Dolphin reloads the archive from disk, so you’ll see your latest save (e.g. new cup or tee position). You don’t need to restart Dolphin or the game.

**If Dolphin doesn’t use the extracted folder:**  
- Make sure the game is actually set to load from that folder (e.g. “Load from Extracted” or a custom path in game properties).  
- Confirm you saved the `.carc` in the correct place (same path you opened from).  
- If you only have a disc image (ISO): extract once as above, then in BrawlCrate always open and save to the extracted path; use Dolphin’s option to prefer/load from that extracted directory if available.

## 5. Naming: Extracted vs Convention

- **From the data:** **Object Group ID** and **Object ID** are read from the PMP at 0x00 and 0x02 in each object entry. The tree shows these as e.g. `(Group 1, ID 0)`.
- **By convention:** The labels **"Tee"** and **"Cup"** are derived from Object ID: `ID 0` → "Tee" (start position), `ID 1` → "Cup" (hole). Any other ID is shown as "Object". This matches observed Wii Sports Golf PMP behavior; if another game or mod uses different IDs, the display name still shows the actual ID in parentheses.

## 6. Changing the Hole Location (Current Workflow)

1. Open the course `.carc` (e.g. `gif_course_fc1.carc`).
2. Expand the `.pmp` node.
3. Select the **Cup** entry to move the hole (e.g. `Cup [115] (Group 1, ID 1)`), or **Tee** to move the start (e.g. `Tee [0] (Group 1, ID 0)`).
4. In the **property grid** (right panel), open the **"PMP Object"** section.
5. Edit **Position** — format is `X, Y, Z`. These are world-space coordinates; change them to move the hole.
6. Save the archive so the game loads the updated PMP.

## 7. Multiple Pin and Tee Objects / How Placement Is Decided

- **Multiple tees (ID 0)** = **multiple start positions** for the same hole. On Wii Golf courses the **cup is typically fixed** for a hole, but there can be **several possible tee (start) locations**. The game may use different tees for different modes or holes.
- **Multiple cups (ID 1)** = multiple pin/hole positions for the same hole. There are **several possible hole (pin) locations** on or around the green. The game likely picks one cup per round (e.g. at random or by day) for variety. So: many Cup objects = many possible pin placements; the game chooses one when you play.
- **How the game picks which tee/cup** is not fully documented. Route/Point data in the PMP (offsets at 0x40+) likely reference which object index is the active tee and pin for a given hole or round. To confirm: compare object counts and order across holes, or test in-game (e.g. replay the same hole and see if the pin position changes).
- For **moving the hole**, edit the **Position** of the **Cup** object(s) (ID 1) you want to change. For **moving the start**, edit the **Tee** object(s) (ID 0). If the game uses a specific cup by index, you may need to move that one; if it picks randomly, any cup you move will affect one of the possible pin positions.

## 8. Next Steps: Visual Editor for Placing the Hole

- **Reference for the 3D view:** Use the **BRRES** model(s) in the same archive (e.g. `gif_course_fc1.brres` / `gif_map_fc1.brres`) as the visual reference. They contain the course geometry and are what BrawlCrate can render.
- **Reference for the hole position:** Use the **PMP** — specifically the **Cup** (ID 1) and **Tee** (ID 0) **Position** vectors already parsed in `PMPNode` / `PMPObjectEntryNode`.
- **Implementation outline:** (1) Reuse or add a 3D viewport that loads and renders the course BRRES. (2) Draw PMP objects (Tee, Cup) as gizmos at their `Position` in the same 3D space. (3) Allow dragging a gizmo to change the selected object's `Position` and write back to the PMP. (4) Optionally sync cup position to the green mesh in the BRRES (Dual-Edit Sync).
- **Which file is the reference:** The **BRRES** is the reference for *drawing* the course; the **PMP** is the reference for *editing* hole/tee placement. Both live in the same `.carc`.

