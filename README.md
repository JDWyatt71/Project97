Team Members

Team Leader
Jack Wyatt

Role:
Created the initial concept and did much of the game design. Organised team meetings and assigned weekly tasks for other members. Assisted on the report and created the presentation plan. Assembled supporting documentation (scrum_board_snapshots, risks, team_members)

Technical Lead 
Jacob Carr

Role:
Coded the game logic, designing the backend architecture; file structures, classes, and implementation of the game mechanics utilising design patterns. Collaborated with Montrel W on the integration of the UI and game logic and Siva S on the user data retrieval system.

Documentation Lead
Joseph Atigogo

Role:
Wrote the report and organised design ideas into a GDD, as well as making the presentation slides and taking meeting notes.

Data Co-Lead 1
Venkata Ashokkumar

Role:
Designed and scripted the seeded dataset, consulting Jack W and Jacob C to verify the validity of assumptions made in its creation. Coded the rule-based suggestion & adjustment slider.

Data Co-Lead 2
Sivasumedhar Sivachidambaram

Role: 
Designed the initial section of the dashboard including structure and prototype views, in addition to scripting the user data retrieval system.

UX Lead
Montrel Williams

Role:
Assisted with game mechanics and designed the user interface and HUD, including the main menu, battle display, and level-up screen. Implemented these aspects and integrated them with the core game mechanics with Jacob C. Also drew sprites.

Testing Lead
Ashlee Chamisa

Role:
Created the automatic test suite, manual end-to-end testing plan, and testing evidence document. Also designed the initial class schemas and wrote the testing guide.


Deployment Guide

Local Game Version
To run game on a windows devices, run the executable: Project97\Builds\Windows Build\v1 Development Build\Project97.exe

Browser Game Version
To run the WebGL build navigate to the build folder with the command prompt by running: cd {FILL IN YOUR PATH}\Project97\Project97\Builds\Web Build\v1 in the command prompt. And then in this directory run: python -m http.server 8000. Now in a browser access with searching: http://localhost:8000.
To access the Console on the web build, right click->Inspect (or F12 shortcut). Then click on the Console page.

Telemetry Dataset Generator
The file Seeded_Dataset_Generator.py, generates the seeded telemetry dataset and write it to a SQLite database.
It produces the following SQLite tables:
sessions
runs
fights
Anomalies_sample

Requirements:
Python 3.10+
Pip installed
Python packages: pip install pandas numpy

How to run:
From the project root directory, bash:
cd Project97/Seeded_Dataset_Final
python Seeded_Dataset_Generator.py

Expected console output:
Wrote SQLite database (with anomalies): Seeded_Dataset.db
Added anomalies_sample table with 150 rows

This will generate:
Seeded_Dataset.db
In the same directory.

Telemetry Dashboard (Streamlit):

The file dashboard.py, visualizes telemetry data.

Requirements
Python 3.10+
Python packages: pip install pandas numpy
Game_telemetry_dash.db to be in the same directory 

How to run
From the project directory, bash:
cd Project97/Data_Presentation1
python Seeded_Dataset_Generator.py

This will launch a local web server, which will automatically open the dashboard in your browser.
Typically at:
http://localhost:8501

Balancing Demonstration Tool (Streamlit)

The file rule_balance.py demonstrates a changeable difficulty parameter which affects combat metrics.

Requirements
Python 3.10+
Python packages: pip install streamlit pandas

How to run
From the project directory, bash:
cd Project97/Data_Presentation1
streamlit run rule_balance.py

This launches an interactive UI in the web browser where difficulty can be adjusted via slider. Average combat metrics update dynamically.
Typically at:
http://localhost:8501

Game Tests

1. Unity Gameplay Tests

Gameplay tests are done using the Unity Test Framework (PlayMode).

Requirements

Unity installed (same version as project)
Project cloned locally

Steps to Run

Open Unity Hub.
Open the Project97 project.
In the Unity Editor, go to:
Window → General → Test Runner
Select the PlayMode tab.
Click Run All.

Expected Result

All automated gameplay tests should pass. These tests cover:
Action Point constraints
Move execution order
Damage and defence calculations
Combat finishing when a character reaches zero HP

Telemetry Tests

Telemetry validation was done using Python and pytest.

Requirements

Python installed
pytest installed
Install using:
pip install pytest

Steps to Run

From the project root directory:
python -m pytest tests/test_telemetry_schema.py -v

Expected Result

The telemetry test should pass successfully.

If the .db file is not present in the project root, the test will skip safely.

