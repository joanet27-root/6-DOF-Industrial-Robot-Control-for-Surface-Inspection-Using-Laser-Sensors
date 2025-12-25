# 6-DOF Industrial Robot Control for Surface Inspection Using Laser Sensors (Unity)

Unity-based simulation of a **6-DoF industrial robot** for **surface inspection with real-time trajectory compensation**.  
The system uses **laser sensor measurements** to reconstruct the inspected surface online and compensates for **dynamic disturbances** affecting the part pose during inspection.

This project focuses on robot modelling, kinematic control, sensor-based feedback, and real-time visualization inside Unity, and is based on the accompanying project report.

---

## Problem Statement
Traditional industrial inspection setups often assume a **static part**, allowing the robot to follow a predefined trajectory. However, in more flexible production environments, the inspected part may experience **translations and rotations** (e.g., due to motion on an external axis), which directly affects inspection accuracy.

Without online compensation, these disturbances lead to deviations between the intended inspection path and the actual TCP trajectory.

---

## Proposed Approach
The proposed control strategy introduces **real-time trajectory correction** based on laser sensor feedback:

1. **Laser sensors** acquire multiple surface points on the inspected part.
2. A **geometric plane** is reconstructed from these points.
3. The **plane normal vector** is computed and used to estimate surface deviation.
4. The estimated disturbance is injected into a **Jacobian-based kinematic control law**.
5. The robot TCP trajectory is corrected online while maintaining the inspection motion.

---

## Method Overview

### Surface Reconstruction
- Laser measurements provide four surface points (**P1, P2, P3, P4**).
- A best-fit plane is computed.
- The plane normal vector represents the deviation of the inspected surface relative to the nominal reference.

### Kinematic Compensation
- The control scheme is based on **Jacobian kinematics**.
- The disturbance derived from the reconstructed plane is introduced as a correction term.
- This enables continuous adjustment of the robot pose in response to surface motion or misalignment.

---

## Results Summary
Comparative simulations show:

- **Without compensation**: the TCP follows the nominal trajectory and does not adapt to disturbances.
- **With compensation**: the TCP follows a corrected trajectory that closely matches the disturbed surface.

These results demonstrate improved robustness and accuracy during inspection tasks under dynamic conditions.

---

## Requirements
- **Unity:** 2022.3.61f1 (LTS)

---

## How to Use the Project

### 1) Clone the repository
```bash
git clone https://github.com/joanet27-root/6-DOF-Industrial-Robot-Control-for-Surface-Inspection-Using-Laser-Sensors.git
cd 6-DOF-Industrial-Robot-Control-for-Surface-Inspection-Using-Laser-Sensors
```

### 2) Open in Unity Hub
1. Open **Unity Hub**
2. Click **Add** and select the repository folder
3. Open the project using **Unity 2022.3.61f1**
4. Allow Unity to import assets and resolve packages

### 3) Run the simulation
1. Open the main scene located in `Assets/Scenes/`
2. Press **Play** to start the simulation

The main scene includes the robot model, laser sensors, and the control logic implementing online compensation.

---

## Notes
- Unity-generated folders such as `Library/` and `Temp/` are intentionally not included in the repository.
- Ensure the correct Unity version is used to avoid package incompatibilities.

---

## Authors
- Joan Carrillo   

---

## Repository Structure
```text
.
├── Assets/                 # Scenes, scripts, robot model, sensors, materials
├── Packages/               # Unity package manifest and lock files
├── ProjectSettings/        # Unity project settings (includes Unity version)
└── README.md
```

---

## Reference
- Project report: *Control de un Robot Industrial de 6 GdL*
