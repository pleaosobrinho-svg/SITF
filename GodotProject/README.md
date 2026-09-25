# SITF — Silence in the Fire

Godot 4.7.2 stable native Android FPS.

This rebuild uses Godot instead of Unity so the project has no proprietary Unity-license dependency. The game builds from procedural geometry and code, so there is no external asset-store dependency.

Target gameplay:
- first-person 3D blocky / low-poly presentation
- six maps
- six weapons
- mobile touch controls and desktop keyboard/mouse
- enemy archetypes with chase, strafe, ranged attacks, patrol and retreat
- muzzle flash, tracers, impact effects and blood particles
- reload, weapon switching, ADS, recoil and hit feedback
- score, kill counter and mission completion screen

Build:
Run the GitHub Actions workflow named "SITF Godot Android APK", then download the artifact named "SITF-Godot-Android-APK". The artifact contains SITF.apk.

Godot 4.7.2 is the current stable 4.7 maintenance release as of September 2026.

CI smoke-test marker: build validation enabled.


Mobile UI revision: landscape launch, virtual joystick, touch aim, menu flow, and SITF launcher icon.

Loading screen and strict script validation added.
