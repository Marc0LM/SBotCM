using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;

namespace SBotCore
{
    public static class ExtensionValue
    {
        static Dictionary<int, LogWriter> loggers = new();
        //static LogWriter logWriter = new("DictEntriesOfInterest.txt"+ Environment.CurrentManagedThreadId);
        static public T Value<T>(this Dictionary<string, object> sender, string key)
        {
            if (sender.ContainsKey(key) && sender[key].GetType() == typeof(T))
            {
                return (T)sender[key];
            }
            else
            {
                //if (loggers.ContainsKey(Environment.CurrentManagedThreadId))
                //{
                //    loggers[Environment.CurrentManagedThreadId].LogWrite(key);
                //}
                //else
                //{
                //    loggers.Add(Thread.CurrentThread.ManagedThreadId, new("DictEntriesOfInterest.txt" + Environment.CurrentManagedThreadId));
                //}
                return default(T);
            }
        }
        static public T Value<T>(this UITreeNode sender, string key)
        {
            return sender.dictEntriesOfInterest.Value<T>(key);
        }
    }
    public class UITreeNode
    {
        public ulong pythonObjectAddress;

        public string pythonObjectTypeName;

        public Dictionary<string, object> dictEntriesOfInterest;
        

        public class DictEntriesOfInterest : Dictionary<string, object>
        {

            public T Value<T>(string key)
            {
                if (this.ContainsKey(key) && this[key].GetType() == typeof(T))
                {
                    return (T)this[key];
                }
                else
                {
                    
                    return default(T);
                }
            }
        }

        public string[] otherDictEntriesKeys;

        public UITreeNode[] children;

        public struct DictEntryValueGenericRepresentation
        {
            public ulong address;

            public string pythonObjectTypeName;
        }

        public struct DictEntry
        {
            public string key;
            public object value;
        }

        public class Bunch
        {
            public List<DictEntry> entriesOfInterest;
        }

        public IEnumerable<UITreeNode> EnumerateSelfAndDescendants() =>
            new[] { this }
            .Concat((children ?? Array.Empty<UITreeNode>()).SelectMany(child => child?.EnumerateSelfAndDescendants() ?? ImmutableList<UITreeNode>.Empty));

        public UITreeNode WithOtherDictEntriesRemoved()
        {
            return new UITreeNode
            {
                pythonObjectAddress = pythonObjectAddress,
                pythonObjectTypeName = pythonObjectTypeName,
                dictEntriesOfInterest = dictEntriesOfInterest,
                otherDictEntriesKeys = null,
                children = children?.Select(child => child?.WithOtherDictEntriesRemoved()).ToArray(),
            };
        }
    }
}
