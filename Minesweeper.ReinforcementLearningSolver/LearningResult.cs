using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SQLite;

namespace Minesweeper.ReinforcementLearningSolver
{
    class LearningResult
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string AlgorithmVersion { get; set; }
        public DateTime CreatedAt { get; set; }

        // Board
        public int BoardWidth { get; set; }
        public int BoardHeight { get; set; }
        public int BombCount { get; set; }

        // Learn
        public int LearnCount { get; set; }
        public TimeSpan LearnTime { get; set; }
        public float LearnStepSize { get; set; }
        public float LearnEpsilion { get; set; }

        // Solve
        public int SolveTrialCount { get; set; }
        public int SolvedCountUnuseLearningData { get; set; }
        public int SolvedCountUseLearningData { get; set; }
    }
}
