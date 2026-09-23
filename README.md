# SITF — Silence in the Fire

Native Unity 6.3 LTS rebuild of the mobile FPS.

## Game
- Landscape-only Android FPS.
- 6 distinct maps: Warehouse, Office, Yard, Hangar, Metro and Block.
- 6 weapons: Pistol, SMG, Rifle, Shotgun, DMR and Sniper.
- Weapon recoil, reload animation, switching animation, ADS and muzzle flash.
- Four enemy archetypes with patrol, chase, strafe, attack, search and retreat states.
- Enemy walk animation, hit reaction and death animation.
- Procedural map geometry: cover, shelves, crates, containers, offices, vehicles, rails, platforms and street props.
- Compact touch HUD: movement joystick, right-side look, fire, ADS, reload and weapon slots.
- Procedural sound generation for firing, reload, hit and footsteps.
- Mobile-focused rendering choices and no paid asset dependency.

## Build
Unity Editor version: 6000.3.0f1.

Open the repository root in Unity Hub and run Assets/Scenes/Main.unity.

For Android, use SITF > Build Android APK in the Unity Editor. The GitHub Actions pipeline is prepared for the same build, but it requires a valid UNITY_LICENSE repository secret. An APK is only considered built after that workflow completes successfully.

The previous Godot prototype is superseded by this Unity project.
