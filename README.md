# Construction Crane Safety Digital Twin: Spatial Telemetry & Predictive AI

[![Open In Colab](https://colab.research.google.com/assets/colab-badge.svg)](https://colab.research.google.com/github/[YOUR_GITHUB_USERNAME]/[YOUR_REPO_NAME]/blob/main/crane_telemetry_analytics.ipynb)
[![Python 3.9+](https://img.shields.io/badge/Python-3.9%2B-blue.svg)](https://www.python.org/)
[![Unity 3D](https://img.shields.io/badge/Unity-2022.3%20LTS-black.svg)](https://unity.com/)

> **An interactive 3D spatial digital twin and telemetry analytics pipeline developed to visualize, audit, and proactively forecast tower crane collision hazards and operator blind spots on congested jobsites.**

---

## 📺 System Demonstration

| 3D Digital Twin Simulation (Unity) | Real-Time Spatial Risk Heatmap (Python) |
| :---: | :---: |
| ![Digital Twin Demo](figures/digital_twin_demo.gif) | ![Risk Heatmap](figures/spatial_risk_heatmap.png) |

> 🎥 **[Watch Full 1-Minute Demonstration Video](https://your-youtube-or-drive-link)**

---

## 📌 Key Highlights & Empirical Results

- **1:1 Physics-Based Digital Twin (Unity C#):** Models a 9.0 m radius crane operating over a 26 m × 26 m jobsite pad with dynamic line-of-sight raycasting and structural occlusion tracking.
- **10 Hz Telemetry Ingestion:** Continuous logging of worker trajectories, payload kinematics, and crane slew angles.
- **Proactive Early-Warning AI:** Random Forest classifier predicting critical blind spot violations **1.0 second in advance** with **0.957 ROC-AUC**.
- **Spatial Optimization:** Dynamic Kernel Density Estimation (KDE) and DBSCAN clustering to automatically position safe ground signaller (Banksman) stations outside the 9.0 m slew envelope.

---

## 🏗️ System Architecture

```text
┌────────────────────────────────────────────────────────┐
│                   Unity 3D Engine                      │
│  - Tower Crane Kinematics (Slew / Trolley / Hoist)     │
│  - 1:1 Physical Scale (26m x 26m Pad, 9.0m Crane Boom) │
│  - Real-time Line-of-Sight & Building Occlusion Rays   │
└──────────────────────────┬─────────────────────────────┘
                           │ 10 Hz Telemetry Stream (.csv)
                           ▼
┌────────────────────────────────────────────────────────┐
│                 Python Analytics Suite                 │
│  1. Ingestion & Kinematic Feature Extraction           │
│  2. Dynamic KDE Multi-Hazard Spatial Heatmapping       │
│  3. DBSCAN Automated Banksman Station Optimization    │
│  4. Random Forest 1.0s Proactive Early-Warning Model   │
└────────────────────────────────────────────────────────┘
