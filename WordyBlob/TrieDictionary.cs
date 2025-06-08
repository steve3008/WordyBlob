using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordyBlob
{
    public class TrieDictionary
    {
        // Trie structure as an array of arrays of integer indices
        UInt32 _iTotalNodes = 0;
        UInt32[][]? _Trie;
        bool[]? _isLeaf;
        char[]? _char;

        public TrieDictionary()
        {
            LoadFromFile("trie_dict.dat").Wait();
        }

        public int NumberOfLeaves(UInt32 curr)
        {
            int count = (_isLeaf != null && _isLeaf[curr]) ? 1 : 0;
            if (_Trie != null)
            {
                foreach (UInt32 i in _Trie[curr])
                {
                    count += NumberOfLeaves(i);
                }
            }
            return count;
        }

        // Searches for the specific word given and returns true if it's in the dictionary.
        public bool SearchForWord(string word)
        {
            UInt32 curr = FindNodeForEndOfWord(word);
            // Return true if the word exists and
            // is marked as ending
            return _isLeaf != null && _isLeaf[curr];
        }

        // Searches for words within the given string. If valid word(s) is/are found, returns
        // length of the longest and outputs the index within the string of its start.
        public int LongestWordInString(string str, ref int iStart)
        {
            int iLen, iMaxLen = 0;
            UInt32 curr;
            int iStrLen = str.Length;
            char c;
            // Starting with each position in the string in turn
            for (int i = 0; i < iStrLen - 1; i++)
            {
                // Initialize the curr pointer with the root node
                curr = 0;
                iLen = 0;
                // Count from this position through the string to the end
                for (int j = i; j < iStrLen; j++)
                {
                    iLen++;
                    c = str[j];
                    curr = NodeContainsChar(curr, c);
                    if (curr == 0)
                        break;
                    // If we've found a word check its length to see if we have a new maximum
                    if(_isLeaf != null && _isLeaf[curr] && iLen > iMaxLen)
                    {
                        iStart = j;
                        iMaxLen = iLen;
                    }
                }
            }
            return iMaxLen;
        }

        UInt32 FindNodeForEndOfWord(string word)
        {
            // Initialize the curr pointer with the root node
            UInt32 curr = 0;

            // Iterate across the length of the string
            foreach (char c in word)
            {
                // Check if the node exists for the current
                // character in the Trie
                curr = NodeContainsChar(curr, c);
                if (curr == 0)
                    return curr;
            }
            return curr;
        }

        // Looks through the children of the given node for the given char
        UInt32 NodeContainsChar(UInt32 curr, char c)
        {
            if (_Trie != null && _char != null)
            {
                foreach (UInt32 i in _Trie[curr])
                {
                    if (_char[i] == c)
                        return i;
                }
            }
            return 0;
        }

        public string[] FindAllWordsWithPrefix(string prefix)
        {
            List<string> words = new List<string>();
            UInt32 curr = FindNodeForEndOfWord(prefix);
            if (curr > 0)
                AddAllWordsToList(curr, prefix, words);
            return words.ToArray();
        }

        void AddAllWordsToList(UInt32 curr, string prefix, List<string> list)
        {
            if (_Trie != null && _char != null && _isLeaf != null)
            {
                foreach (UInt32 i in _Trie[curr])
                {
                    string word = prefix + _char[i];
                    if (_isLeaf[i])
                        list.Add(word);
                    AddAllWordsToList(i, word, list);
                }
            }
        }

        public async Task LoadFromFile(string fileName)
        {
            using Stream fileStream = await FileSystem.Current.OpenAppPackageFileAsync(fileName);
            {
                using (var reader = new BinaryReader(fileStream, Encoding.UTF8, false))
                {
                    _iTotalNodes = reader.ReadUInt32();
                    _Trie = new UInt32[_iTotalNodes][];
                    _isLeaf = new bool[_iTotalNodes];
                    _char = new char[_iTotalNodes];
                    int iLength;
                    for (int i = 0; i < _iTotalNodes; i++)
                    {
                        _isLeaf[i] = reader.ReadBoolean();
                        _char[i] = reader.ReadChar();
                        iLength = reader.ReadInt32();
                        _Trie[i] = new UInt32[iLength];
                        for (int j = 0; j < iLength; j++)
                        {
                            _Trie[i][j] = ((UInt32)reader.ReadByte() << 16) |
                                          ((UInt32)reader.ReadByte() << 8) |
                                           (UInt32)reader.ReadByte();
                        }
                    }
                }
            }
            fileStream.Close();
            fileStream.Dispose();
        }
    }
}
