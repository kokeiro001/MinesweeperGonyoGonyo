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
        public int LearnCount { get; set; }

        public int SolveTrialCount { get; set; }
        public int SolvedCount { get; set; }
    }
}
