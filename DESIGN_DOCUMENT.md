# Psyche Mission Strategy Game 
## Roguelike Card Game - Design Document 

### 1. Game Overview 
*   **Title**: Psyche Mission Strategy Game 
*   **Genre**: Roguelike deck-building strategy game 
*   **Platform**: Web-based (Unity WebGL), fully offline 
*   **Target Audience**: Students and lifelong learners, ages 12 and above 
*   **Session Length**: 20-35 minutes per complete run (no saving between sessions) 

### 2. Core Mechanics 
*   **Roguelike Progression**: Players progress through 4 mission floors with randomized encounters. Each run is self-contained with no persistent progression between runs. 
*   **Deck-Building**: Players start with a basic deck and add one card after each encounter, building a customized strategy throughout the run. 
*   **Resource Management**: Three finite resources must be balanced: 
    *   **Power** (refreshes each encounter)
    *   **Budget** (persistent and scarce)
    *   **Time** (persistent countdown to mission end)
*   **Encounter System**: Each encounter presents a specific challenge (collect data, manage crisis, resource optimization). Playing cards costs resources and advances toward the encounter goal. 
*   **Crisis Response**: Random crisis events occur during encounters, requiring strategic adaptation to avoid penalties or failure. 
*   **Evidence-Based Science**: Players collect raw data with Instrument cards, then synthesize it into Scientific Conclusions using Analysis cards, mirroring the real scientific method. 

### 3. Themes and Goals 
*   Educational simulation of real NASA mission planning and execution 
*   Authentic scientific instruments and mission constraints based on Psyche documentation 
*   Professional, educational, non-violent tone appropriate for all ages 
*   Emphasize strategic thinking, resource tradeoffs, and evidence-based reasoning 

### 4. User Interface 
*   **Resource Display**: HUD shows current Power, Budget, and Time resources 
*   **Card Hand**: Display of 5 cards drawn from deck, with drag-to-play interaction 
*   **Encounter Objective**: Clear display of current encounter goal and progress 
*   **Map View**: Between encounters, shows available paths and upcoming encounter types 
*   **Reward Selection**: After encounters, choose 1 of 3 card rewards to add to deck 
*   **Design**: Unity Canvas system ensures browser compatibility (Chrome, Firefox, Edge, Safari) 

### 5. Technical Specifications 
*   **Engine**: Unity 2021+ with C# scripting 
*   **Architecture**: Modular component-based design (card system, resource tracker, encounter manager all independent) 
*   **Data Management**: Scriptable Objects for card definitions, JSON for encounter configurations 
*   **Performance Target**: 30+ FPS, initial load under 15 seconds 
*   **Build**: WebGL export, fully client-side, no server required 
*   **Save System**: None required - each run is self-contained 

### 6. Win Condition 
**Victory**: Complete Floor 4 (Final Mission Review) by presenting 3 Scientific Conclusions within the turn limit. 

**How to Win**: 
*   Progress through 4 mission floors, defeating 2 “bosses” along the way 
*   Collect raw data using Instrument cards 
*   Synthesize data into Conclusions using Analysis cards 
*   Present 3 Conclusions in the final “boss” encounter 

**Loss Conditions**: 
*   Fail any “boss” encounter objective 
*   Run out of Time (persistent resource reaches zero) 
*   Run out of Budget with no way to progress 

### 7. Floor Structure 
The game consists of 4 floors representing different mission phases. Each floor contains multiple encounters, with boss encounters at critical milestones. 

| Floor | Mission Phase | Encounters | Goal |
| :--- | :--- | :--- | :--- |
| **Floor 1** | **Cruise Phase** | 3 encounters (6-9 min) | Build deck, gather resources |
| **Floor 2** | **Orbit Insertion (BOSS)** | 1 boss (5-8 min) | Accumulate 10 Power + spend 5 Budget within 8 turns |
| **Floor 3** | **Science Operations** | 4 encounters (8-12 min) | Collect data, create Conclusions |
| **Floor 4** | **Mission Review (BOSS)** | 1 boss (8-12 min) | Present 3 Conclusions within 10 turns = WIN |

**Total Run Time**: 25-40 minutes for a complete run (fits within 20-40 minute target) 

### 8. Starting Conditions 
**Starting Resources**: 
*   **Power**: 3 (refreshes to 3 at the start of each encounter) 
*   **Budget**: 6 (persistent throughout run, difficult to replenish) 
*   **Time**: 15 (persistent countdown, reaching 0 = instant loss) 

**Starting Deck (10 cards)**: 
*   4x Solar Array Deploy 
*   2x Budget Request 
*   2x Multispectral Imager 
*   1x Trajectory Correction 
*   1x Compositional Analysis 

**Card Acquisition**: 
*   After each encounter: Choose 1 of 3 randomly offered cards 
*   After boss encounters: Choose from rarer cards OR remove 1 card from deck 
*   Deck size grows throughout run (typically 15-20 cards by end) 

### 9. Encounter Flow 
**Setup Phase**: 
*   Shuffle deck and draw 5 cards 
*   Power resets to 3 
*   Display encounter objective (varies by encounter type) 

**Play Phase**: 
*   Play cards from hand by paying resource costs 
*   Each turn: play any number of cards, then draw back to 5 
*   Continue until encounter objective is met or failure occurs 
*   When deck runs out, shuffle discard pile to form new deck 

**Victory Phase**: 
*   Objective completed successfully 
*   Choose 1 card reward from 3 options 
*   View map and select next encounter 

**Defeat Phase**: 
*   Encounter objective failed. Run ends, restart from Floor 1. 

### 10. Card Catalog 
All cards are organized by category. Cost values may be adjusted during playtesting for balance. 

#### Resource Cards 
Generate Power, Budget, or Time. Essential for maintaining operations throughout the run. 
*   **Solar Array Deploy** (Common): Cost 0. Gain 3 Power.
*   **Budget Request** (Common): Cost 1 Time. Gain 3 Budget.
*   **Mission Extension** (Uncommon): Cost 3 Budget. Gain 5 Time.
*   **Nuclear Battery** (Rare): Cost 2 Budget. Gain 2 Power every turn (permanent).
*   **Power Conservation** (Uncommon): Cost 1 Time. All Power costs reduced by 1 this turn.
*   **Emergency Fund** (Uncommon): Cost 0 (Crisis only). Gain 2 Budget immediately.

#### Instrument Cards 
Deploy scientific instruments to collect data. Based on real Psyche spacecraft payload. 
*   **Multispectral Imager** (Common): Cost 2 Power, 1 Time. Collect 2 Surface data.
*   **Gamma-Ray Spectrometer** (Uncommon): Cost 3 Power, 2 Time. Collect 3 Elemental data.
*   **Magnetometer** (Uncommon): Cost 2 Power, 2 Time. Collect 2 Magnetic data.
*   **X-band Radio** (Common): Cost 1 Power, 3 Time. Collect 3 Gravity data.
*   **Deep Space Optical Comms** (Rare): Cost 3 Power, 1 Time. Draw 2 cards.
*   **Multi-Instrument Suite** (Rare): Cost 4 Power, 2 Time. Collect 1 of each data type.

#### Maneuver Cards 
Execute trajectory changes and orbital positioning. Critical for mission success. 
*   **Trajectory Correction** (Common): Cost 2 Power, 1 Budget. Adjust orbital phase.
*   **Orbit Insertion Burn** (Rare): Cost 5 Power, 3 Budget. Enter stable orbit (enables advanced instruments).
*   **Altitude Adjustment** (Uncommon): Cost 3 Power, 2 Budget. Move altitude bands (+1 bonus to next Instrument).
*   **Reaction Wheel Reset** (Common): Cost 1 Power. Prevent penalty on next Instrument card.
*   **Close Approach Flyby** (Uncommon): Cost 4 Power, 2 Budget. Next Instrument collects double data (Risk: Crisis).
*   **Safe Mode Recovery** (Uncommon): Cost 2 Power, 2 Time. Cancel 1 active Crisis effect.

#### Analysis Cards 
Process raw data into scientific conclusions. Evidence-based reasoning required to win. 
*   **Compositional Analysis** (Common): Cost 2 Time, 1 Budget. Convert 3 Elemental + 2 Surface → 1 Composition Conclusion.
*   **Magnetic Modeling** (Uncommon): Cost 3 Time, 2 Budget. Convert 4 Magnetic → 1 Dynamo Conclusion.
*   **Structural Study** (Common): Cost 2 Time, 1 Budget. Convert 3 Gravity + 2 Surface → 1 Interior Conclusion.
*   **Thermal Reconstruction** (Rare): Cost 4 Time, 2 Budget. Convert 2 of each data type → 1 Formation Conclusion.
*   **Comparative Planetology** (Uncommon): Cost 2 Time, 2 Budget. Convert any 5 data → 1 Conclusion of choice.
*   **Peer Review Publication** (Rare): Cost 3 Time, 3 Budget. Upgrade 1 Conclusion to count as 2.

#### Crisis Cards 
Unexpected mission challenges. Must be resolved to avoid ongoing penalties. Crisis cards are added to your deck during certain encounters. 
*   **Solar Storm Warning** (Common): Penalty: Lose 2 Power per turn. Resolve: Pay 3 Power OR enter safe mode.
*   **Thruster Anomaly** (Uncommon): Penalty: All Maneuver cards cost +1 Power. Resolve: Pay 2 Budget and 2 Time.
*   **Ground Station Conflict** (Common): Penalty: Cannot draw cards for 1 turn. Resolve: Pay 2 Budget.
*   **Data Storage Full** (Common): Penalty: Cannot collect data until resolved. Resolve: Pay 2 Time to downlink.
*   **Debris Field Detected** (Uncommon): Penalty: Next Maneuver fails unless resolved. Resolve: Pay 4 Power (no turn lost).
*   **Budget Cut Notice** (Uncommon): Penalty: Lose 3 Budget immediately. Resolve: Pay 3 Time to restore 2 Budget.
*   **Computer Reboot Required** (Rare): Penalty: Skip next turn entirely. Resolve: Pay 3 Power and 1 Budget.

### 11. Data Types and Conclusions 
The game uses a two-tier scientific system: raw data collected by instruments, and conclusions synthesized from data through analysis. 

**Raw Data Types (Tier 1)**: 
*   **Surface**: Optical imaging, topography, surface features
*   **Elemental**: Chemical composition via spectroscopy 
*   **Magnetic**: Magnetic field measurements 
*   **Gravity**: Mass distribution via Doppler tracking 
*   **Thermal**: Temperature and heat properties 

**Scientific Conclusions (Tier 2)**: 
*   **Composition Conclusion**: Material makeup of Psyche 
*   **Dynamo Conclusion**: Ancient magnetic field evidence 
*   **Interior Conclusion**: Internal structure (solid vs differentiated) 
*   **Formation Conclusion**: How Psyche formed and evolved 

Note: The final boss requires presenting 3 Conclusions, not raw data. This emphasizes the scientific method: collect evidence, then draw conclusions. 

### 12. Encounter Types 
Non-boss encounters vary in objectives and rewards. Players choose their path through the map. 

| Encounter Type | Objective | Typical Reward |
| :--- | :--- | :--- |
| **Data Collection** | Collect X data points of specific types | Choice of Instrument cards |
| **Resource Management** | Survive X turns with limited resources | Choice of Resource cards |
| **Crisis Response** | Resolve Crisis card within turn limit | Choice of Maneuver/Support cards |
| **Analysis Challenge** | Create 1+ Conclusions from available data | Choice of Analysis cards |
| **Random Event** | Variable - may gain/lose resources, add cards, etc. | Immediate effect (no card choice) |

### 13. Implementation Notes 
**Card Data Structure**: 
*   Use Unity Scriptable Objects for each card 
*   Properties: Name, Type, Cost (Power/Budget/Time), Effect, Rarity, Art Asset 
*   Effect as method reference for flexible implementation 

**Encounter Configuration**: 
*   JSON files define encounter objectives, turn limits, rewards 
*   Floor structure defines encounter pool for each floor 
*   Random selection ensures variety across runs 

**Balance Testing Priorities**: 
*   Session length: 20-35 minutes average 
*   Win rate: 30-50% for experienced players 
*   Resource scarcity forces meaningful choices without frustration 
*   Multiple viable deck strategies (instrument-heavy, analysis-focused, balanced) 
*   Crisis cards challenging but not game-ending 

**Educational Goals**: 
*   Players learn real Psyche mission instruments and objectives 
*   Understand resource constraints in space missions 
*   Practice evidence-based reasoning (data → conclusions) 
*   Experience strategic planning under uncertainty 
*   Appreciate scientific method through gameplay
