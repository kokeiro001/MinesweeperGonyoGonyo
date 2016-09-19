﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minesweeper.ReinforcementLearningSolver
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    enum ActionType
    {
        Open,
        OpenEight,
        Flag
    }

    class ActionClass
    {
        public ActionType ActionType;
        public int Y;
        public int X;
    }

    class ValueClass
    {
        static float stepSize = 0.1f;

        Dictionary<StateClass, double> value = new Dictionary<StateClass, double>();

        public double Get(StateClass state)
        {
            return value[state];
        }

        public ActionClass GetMaxAction(StateClass state)
        {
            // TODO
            var actions = state.ValidActions;
            return null;
        }

        public void Update(StateClass state, double reward, StateClass nextState)
        {
            double nextStateValue = 0;
            if(nextState == null)
            {
                nextStateValue = 0;
            }
            else
            {
                nextStateValue = value[nextState];
            }
            value[state] += stepSize * (reward + nextStateValue - value[state]);
        }
    }

    class StateClass
    {
        public StateClass(int[] board)
        {
            // TODO: copy
        }

        public StateClass Action(ActionClass action)
        {
            StateClass newState = null; // TODO: Clone
            // open board and copy 
            return null;
        }

        public int MakeHash()
        {
            return 0;
        }


        public ActionClass[] ValidActions => null; // TODO: 
    }

    class QLearningCom {

        static Random random = new Random();
        static double epsilon = 0.1;

        ValueClass value;
        public bool learning { get; private set; }

        double previousReward;
        StateClass previousAfterState;

        void Initialize(ValueClass value, bool learning)
        {
            this.value = value;
            this.learning = learning;

            previousReward = -1;
            previousAfterState = null;
        }

        public ActionClass SelectAction(StateClass state)
        {
            var maxAction = value.GetMaxAction(state);
            var selectedAction = maxAction;


            if(learning)
            {
                // 推定方策（グリーディ）で直前の行動の価値を学習
                StateClass afterState = state.Action(maxAction);

                if(previousReward != -1 && previousAfterState != null)
                {
                    value.Update(previousAfterState, previousReward, afterState);
                }


                // 挙動方策（ε-グリーディ）で行動を決定
                if(random.NextDouble() < epsilon)
                {
                    int randomIndex = random.Next(state.ValidActions.Length);
                    selectedAction = state.ValidActions[randomIndex];
                }
                previousAfterState = state.Action(selectedAction);
            }
            return selectedAction;
        }

        public void Learn(double reward, bool finished)
        {
            if(learning)
            {
                if(finished)
                {
                    // 終端状態の場合、学習する機会がここしかないので、
                    // ここで学習する
                    value.Update(previousAfterState, reward, null);
                    previousReward = -1;
                    previousAfterState = null;
                }
                else
                { 
                    previousReward = reward;
                }
            }
        }

    }
}
