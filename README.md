## Unit and Integration Test Documentation

Unit testing is done through Unity PlayMode tests covering all core molecule reactions.
All the test files are located in Assets/Tests/.
In addition to unit testing, we rigorously did integration and end-to-end testing by manually running the sandbox through our VR headset, testing all combinations of molecules, placements, stress testing, etc, from early development to the current production-ready stage. Making sure that what our end user will receive is of quality.

Unity PlayMode tests made it so that we can spawn molecules, move them, all automated with scripts. The molecules’ positions, movements, and inputs are the exact same every time. We just need to monitor to see if the correct reaction happens.

### EmulsifierReactionPlayModeTests.cs

#### Tests that emulsifier molecules correctly bind to the right partners.

- EmulsifierHydrophilicAttached - A water molecule snaps onto the hydrophilic end of an emulsifier

- EmulsifierHydrophobicAttached - A lipid molecule snaps onto the hydrophobic end of an emulsifier

- EmulsifierHydrophobicAndHydrophobicAttached - Both a water and a lipid molecule attach to their correct ends at the same time

### LipidReactionPlayModeTests.cs

#### Tests lipid merging and how lipids behave near water.

- LipidMerge_FallingLipid_MergesAndGrowsGroundLipid - Two lipids collide, they join into one bigger lipid, and only one remains in the scene

- WaterNearMergedLipid_RepelsLipidAway - A lipid placed near water gets pushed away, and the gap between them grows over time

### PolysaccharidePlayModeTests.cs

#### Tests how polysaccharides change when touch water, heat, and cold.

- PolysaccharideSwelling - A polysaccharide absorbs a nearby water molecule, and the water disappears

- PolysaccharideGelatinization - A water-swelled polysaccharide placed in a heat zone transforms into an AmyloseGel object

- PolysaccharideRetrogradation - A swelled polysaccharide placed in a fridge zone shrinks back to its original size

### ProteinReactionPlayModeTests.cs

#### Tests how proteins denature and bond with each other.

- ProteinDenatureByItself - Manually triggering denaturation on a protein correctly changes its state

- ProteinDenatureByHeatZone - A protein left inside a heat zone becomes denatured after enough time passes

- ProteinBonding - Two denatured proteins that collide lock together into a bonded pair

- TwoProteinDenaturedAtTheSameTimeThenBonded - Two proteins heated in the same zone at the same time still bond correctly when they meet

## End-to-end Test Documentation

Due to the nature of this project, we did not implement end-to-end testing in code and explained our end-to-end testing methodology in compiled a Google Docs file. https://docs.google.com/document/d/1f7zd_kiJqH-DPjiuuQl1FLrceGvSZPQhlnrKTx-aq2U/edit?usp=sharing

This document is also uploaded as part of our final deliverables.

---

## Code Documentation

Core scripts consisting of molecule reactions, systems, and UI written in C#.
Built in Unity using Meta's XR SDK.

### Molecule.cs (Base Class)

- Every molecule type in the game builds on top of this script, basically, the parent class in OOP language
- Stores the molecule's name and description, and handles the visual highlight when selected
- GetDescription() / GetMoleculeSO() / GetState() - return molecule info; GetState() is overridden by each specific molecule type to reflect its current condition (e.g. "Native", "Denatured")

### Lipid.cs

- Controls what happens when two lipid molecules run into each other - they merge into one bigger one
- Only one of the two lipids kicks off the merge to make sure it doesn't happen twice
- Merge(Lipid other) - locks both lipids so nothing else interferes, then starts the merge animation
- MergeAnimation() - moves both lipids together, grows one, shrinks and removes the other, then fires off a reaction notification

### Water.cs

- Makes water molecules push lipids away (they don't mix)
- The pushing happens on a schedule to keep performance smooth
- Repel(Collider other) - physically pushes a lipid directly away from the water molecule

### Protein.cs

- Proteins go through three stages: normal → denatured → bonded
- The visual change during denaturation is done by morphing the 3D mesh shape
- SetDenatureWeight(float) - updates how denatured the protein looks; fully triggers denaturation once it hits a threshold
- Bonding(Protein other) - physically locks two denatured proteins together and draws a visual connection between them

### Emulsifier.cs

- An emulsifier has two ends - one that attracts water and one that attracts fat
- This script is the identifier; the actual snapping behaviour lives in EmulsifierSnapPoint.cs
- GetEmulsifierSnapReactionSO() - returns the event that triggers the glow effect when a molecule snaps on

### PolysaccharideSwelling.cs

- Makes polysaccharides visually swell up when they absorb water, and shrink back down in the fridge
- SwellBeads() - slowly scales up the inner glucose objects and removes the water molecule from the scene
- DeswellBeads() - slowly shrinks those objects back down when inside a fridge zone

### PolysaccharideGelatinization.cs

- Handles the transformation from a swelled polysaccharide into a gel when exposed to heat
- GelatinizeSequence() - hides the original molecule, spawns the gel object in its place, then removes the original

### HeatZone.cs

- A zone that gradually denatures any protein inside it, and gelatinizes any swelled polysaccharide that enters
- Tracks all proteins inside it and increases their denaturation level each frame until they fully denature

### StateManager.cs

- A global tracker that tracks every molecule existing in the scene
- RegisterMolecule(GameObject) - adds a new molecule to the tracking list when it spawns
- DestroyAll() - wipes every molecule from the scene, used for resetting
  ReactionEvents.cs
- A simple notification system so that different scripts can react to chemistry events without being directly connected
- Raise(ReactionSO) - broadcasts that a reaction happened; anything listening (like the glow effect) responds automatically

### GazeInspectionController.cs

- Detects which molecule the user is looking at by shooting an invisible ray from the camera
- When a molecule is in the user's gaze, its info panel appears on screen

### WristMenu.cs

- Sticks a UI menu to the user's left wrist in VR
- DeleteAll() - resets the scene by calling the StateManager
- OpenMenu() / CloseMenu() - shows or hides the menu with a small animation

### Other Files

The above is not an exhaustive list of our development files. The following is a list of directories that contain the files and scripts directly added by us. 

(All of these are inside the Assets/ directory)
Prefabs/
Scenes/
Scripts/
Sprites/
Tests/

Note: OneGrabFreeTransformerMolecule.cs and Protein/GrabFreeTranformerProtein.cs in Scripts/Molecules are APK library files modified to meet our needs and not written originally by us.

