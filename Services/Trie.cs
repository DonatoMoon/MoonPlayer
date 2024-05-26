using MoonPlayer.Models;
using System.Collections.Generic;

namespace MoonPlayer.Services
{
    public class TrieNode
    {
        public Dictionary<char, TrieNode> Children = new Dictionary<char, TrieNode>();
        public List<AudioFile> AudioFiles = new List<AudioFile>();
    }

    public class Trie
    {
        private TrieNode root = new TrieNode();

        public void Insert(string word, AudioFile audioFile)
        {
            var current = root;
            foreach (var letter in word)
            {
                if (!current.Children.ContainsKey(letter))
                {
                    current.Children[letter] = new TrieNode();
                }
                current = current.Children[letter];
            }
            current.AudioFiles.Add(audioFile);
        }

        public List<AudioFile> Search(string prefix)
        {
            var current = root;
            foreach (var letter in prefix)
            {
                if (!current.Children.ContainsKey(letter))
                {
                    return new List<AudioFile>();
                }
                current = current.Children[letter];
            }

            return CollectAllAudioFiles(current);
        }

        private List<AudioFile> CollectAllAudioFiles(TrieNode node)
        {
            var result = new List<AudioFile>();

            if (node.AudioFiles.Count > 0)
            {
                result.AddRange(node.AudioFiles);
            }

            foreach (var child in node.Children.Values)
            {
                result.AddRange(CollectAllAudioFiles(child));
            }

            return result;
        }
    }
}
