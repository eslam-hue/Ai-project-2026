                                                       Egyptian League Scheduling System (CSP Solver)

Project Overview

This project is an AI-based scheduling system for the Egyptian football league. It models the scheduling problem as a **Constraint Satisfaction Problem (CSP)** and generates a complete, conflict-free fixture list for all teams.

The system assigns matches to valid dates, times, and stadiums while satisfying multiple real-world constraints such as rest periods, stadium availability, and match distribution rules.


AI Concept Used

This project is based on:

* **Constraint Satisfaction Problem (CSP)**
* **Greedy Search with Heuristics**
* **Forward Checking (partial via incremental validation)**
* 
Key Features

* Full Round-Robin schedule generation
* Home and away match balancing
* Time window-based scheduling
* Constraint validation:

  * 72-hour rest rule for teams
  * Stadium time conflict prevention
  * Maximum matches per day
* Greedy assignment with heuristics
* State tracking for teams and stadiums

 System Architecture

1. Match Generation

* Uses Round Robin algorithm to generate all fixtures
* Creates both legs (home & away matches)

2. Scheduling Layer

* Assigns matches to time windows (not fixed dates)
* Interleaves rounds for realism

3. Constraint Engine

* Validates:

  * Team rest time (72 hours)
  * Stadium availability
  * Daily match limits

4. Assignment Strategy

* Greedy selection
* Heuristic-based ordering (least loaded days first)
* Incremental validation using temporary state


Main Data Structures

* `TEAMS`: List of all participating teams
* `STADIUMS`: Mapping team → home stadium
* `team_last_match`: tracks last match date per team
* `stadium_day_times`: prevents time conflicts per stadium
* `temp`: tracks in-progress assignments per round
* 
1. Generate round-robin matches
2. Create home & away legs
3. Interleave rounds
4. For each round:

   * Generate time window
   * Assign matches greedily
   * Validate constraints
   * Update global state
5. Return final schedule

 Cmplexity

* Time Complexity: Approx. O(M × D × T)

  * M = number of matches
  * D = candidate days
  * T = time slots

* Optimized using heuristics to reduce search space

 Design Decisions

* Chose **Greedy over Backtracking** for performance reasons
* Used heuristics to improve solution quality
* Accepted trade-off between completeness and efficiency

 Limitations

* Not a complete CSP solver (no full backtracking)
* No guarantee of optimal schedule in all cases
* May fail under highly constrained scenarios


 Future Improvements

* Add backtracking or hybrid solver
* Introduce optimization function (utility-based scoring)
* Improve fairness metrics (home/away balance)
* Integrate UI/API for real-time scheduling
