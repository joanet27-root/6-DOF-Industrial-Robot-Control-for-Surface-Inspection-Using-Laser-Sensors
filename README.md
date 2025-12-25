# 6-DOF Industrial Robot Control for Surface Inspection Using Laser Sensors (Unity)

Unity-based simulation of a **6 DoF industrial robot** for surface inspection. The robot follows an inspection trajectory and **dynamically compensates disturbances in real time** using measurements from **laser sensors** (surface/plane estimation → trajectory correction).

This project focuses on robot modelling, kinematics/control logic, and sensor-driven compensation strategies for robust inspection tasks.

---

## Key Features
- 6 DoF robot simulation in Unity
- Laser sensor-based surface measurement
- Online trajectory adjustment / compensation for disturbances
- Real-time visualization and interaction inside Unity

---

## Requirements
- **Unity:** `2022.3.61f1` (LTS)

---

## Repository Structure
```text
.
├── Assets/                 # Scenes, scripts, materials, objects
├── Packages/               # Unity package manifest and lock
├── ProjectSettings/        # Unity project settings (includes Unity version)
└── README.md
