using System;
using System.IO;
using UnityEngine;

namespace PingPong.Gameplay
{
    public sealed class JsonFileStore<T> : IKeyedStore<T> where T : class
    {
        private readonly string _path;

        public JsonFileStore(string directory, string fileName)
        {
            _path = Path.Combine(directory, fileName);
        }

        public T Load()
        {
            try
            {
                return File.Exists(_path) ? JsonUtility.FromJson<T>(File.ReadAllText(_path)) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Save(T value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            var temporary = _path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(value));
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }

            File.Move(temporary, _path);
        }

        public void Clear()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
    }
}
