using System.Collections.Generic;
using System.Diagnostics;

namespace Minesweeper.ReinforcementLearningSolver
{
    class UlongsDictionary<T>
    {
        readonly int ArraySize = 2;

        Dictionary<ulong, Dictionary<ulong, T>> dictionary = new Dictionary<ulong, Dictionary<ulong, T>>();

        public void Add(ulong[] key, T value)
        {
            Debug.Assert(key.Length == ArraySize);
            Debug.Assert(!(dictionary.ContainsKey(key[0]) && dictionary[key[0]].ContainsKey(key[1])));

            if(!dictionary.ContainsKey(key[0]))
            {
                dictionary.Add(key[0], new Dictionary<ulong, T>());
            }

            dictionary[key[0]].Add(key[1], value);
        }

        public bool ContainsKey(ulong[] key)
        {
            return dictionary.ContainsKey(key[0]) && dictionary[key[0]].ContainsKey(key[1]);
        }

        public T Get(ulong[] key)
        {
            return dictionary[key[0]][key[1]];
        }

        public IEnumerable<ulong[]> CalculateKeys()
        {
            foreach(var item in dictionary)
            {
                foreach(var item2 in item.Value)
                {
                    yield return new ulong[]
                    {
                        item.Key,
                        item2.Key
                    };
                }
            }
        }
    }

    // BigIntegerでディクショナリとか作るのはどうかしら？Redisとかの利用の検討はどうかしら？
}
