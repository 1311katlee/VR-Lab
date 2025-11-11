# VR Flocculation Coagulation Lab

A Unity3D virtual reality application that replicates the PB700 flocculation-coagulation water treatment laboratory experiment for environmental engineering students. Students conduct a two-phase experiment to optimize water treatment by first identifying ideal pH levels, then determining optimal coagulant dosage.

## Overview

This project provides an immersive VR simulation of the water coagulation and flocculation process, allowing students to conduct systematic experiments and learn about water treatment principles in a safe, interactive, and repeatable virtual environment. The simulation accurately models the chemical and physical processes involved in removing impurities from water through a structured learning progression.

### Educational Workflow

The lab uses a **locked variable** approach to guide student learning:

1. **Phase 1: pH Optimization Lab (Alum Locked)**
   - Coagulant dosage is locked at optimal amount
   - Student controls pH levels
   - Observes how pH affects water clarity
   - Identifies ideal pH for maximum coagulation

2. **Phase 2: Coagulant Dosage Lab (pH Locked)**
   - pH is locked at the ideal value discovered in Phase 1
   - Student controls alum (coagulant) dosage
   - Observes how dosage affects treatment efficiency
   - Identifies optimal alum concentration

This pedagogical structure ensures students understand the relationship between each variable and treatment effectiveness before moving to more complex scenarios.

## Key Features

- **Two-Phase Progressive Learning**: Structured curriculum that builds understanding systematically
- **pH Optimization Experiment**: Variable pH with locked coagulant dosage to demonstrate pH effects
- **Coagulant Dosage Optimization**: Variable dosage with locked optimal pH to find ideal treatment concentration
- **Realistic Simulation**: Accurate modeling of coagulation and flocculation physics and chemistry
- **Interactive Experiments**: Students perform realistic lab procedures with real-time feedback
- **Real-time Water Clarity Visualization**: Particles visibly aggregate and settle based on parameters
- **Quantitative Measurements**: Turbidity (NTU), pH readings, and efficiency calculations
- **Educational Scaffolding**: Guided experiments with explanations of chemical processes
- **Repeatable**: Run experiments unlimited times without resource waste
- **Cost-Effective**: Eliminates need for physical lab materials and chemical reagents
- **Safe Environment**: No chemical exposure or safety hazards

## Technical Details

### Technology Stack
- **Engine**: Unity3D (2021.3 LTS or later)
- **Platform**: VR
- **Language**: C#
- **XR Framework**: XR Interaction Toolkit
- **Version Control**: Git

## Long-Term Goals
- Expand to full water treatment plant simulation
- Add temperature effects on coagulation
- Include organic contamination scenarios
- Develop assessment rubric integration
- Create 5+ water quality scenarios

## System Requirements & Technical Specs

### Recommended Hardware (Optimal Performance)
- **Headset**: Meta Quest 3
- **PC**:
  - CPU: Intel i7-9700K or AMD Ryzen 3700X
  - RAM: 16GB
  - GPU: NVIDIA RTX 2070 or RTX 3060
  - Storage: 10GB NVMe SSD
  - USB 3.0+ port
- **Performance**: Stable 90 FPS, comfortable for extended use


## Authors & Acknowledgments

### Development Team
- **Lead Developer**: Ethan Tan
- **Thesis Student**: Katrina Lee
- **Environmental Engineering Advisor**: Amro el Badawy
