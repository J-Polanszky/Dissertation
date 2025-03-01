# Dissertation

Dynamic Difficulty adjustment aided by Reinforcement Learning in Strategy games.

# TODO List

<!-- - Set up .yaml config file for Agent - 0.5 : Done by the 23rd Feb - Transfered everything on main pc to ubuntu to solve python issues -->
<!-- - Set up rewards and punishments for score - 0.5-1 : Done by the 23rd Feb -->
- Set up tensor board and such for agent monitoring - 1 : Done by the 2nd Mar - Tensorboard is setup as part of the training, but stats recorder is to be fine tuned.
<!-- - Decide on what RL agent/SM agent data during training would be best for the DDA system - 2-4 : Done by the 2nd March - Likely will be player score, inventory, and the total number of score stored in inventory, and only from the RL Agent. this will be used to set a comparison for the decision tree to realise which RL Agent the player is performing most similarly to --> 
- Fix mining bug with rl agent.
- Fix animation bug, but not needed to be done during training.
- Implement logging and data gathering for DDA system in training scene - 1-2 : Done by the 2nd March - Change of plan. To better train the DDA model, instead the 5 selected rl models for difficulty will be taken, and data gathered from their evaluation scene runs (probably about 10-20 games). Then, some of the previous and future models will be evaluated to help bolster the data set and account for edge cases. some of these will be used as validation data to test and ensure the dda model works as intended. Then i will play a few games, gathering my own data and the state machines, and testing the dda system with that, along with another play test to see if it feels right. LightBGM will be used.
- Ensure that everything is ready for training, monitoring, extraction. Then make evaluation scene. - 1-2 : Done by the 2nd March
- Everything is ready to train the RL Agent. Expected training duration needed (maximum) - 30 hours, split across 3 weeks. : Start by the 3rd March. End by 23rd March latest
- DDA system should not take longer than 2-3 weeks to develop and implement, and final testing of the prototype and data collection scripts added by end of 2nd week of April, with end of April being the absolute limit if things go poorly. Goal is to allow 2-3 weeks in april and may for assignments, and cleaning up/finalising the earlier parts of the paper, and leaving 3 weeks for evaluation, conclusion and abstract.