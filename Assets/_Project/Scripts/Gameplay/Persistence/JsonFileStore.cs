using System;
using System.IO;
using UnityEngine;

namespace PingPong.Gameplay
{
    public sealed class JsonFileStore<T> : IKeyedStore<T> where T : class
    {
        private readonly string _path;
        private readonly string _temporaryPath;

        public JsonFileStore(string directory, string fileName)
        {
            _path = Path.Combine(directory, fileName);
            _temporaryPath = _path + ".tmp";
        }

        public T Load()
        {
            try
            {
                var source = File.Exists(_path) ? _path : File.Exists(_temporaryPath) ? _temporaryPath : null;
                return source != null ? JsonUtility.FromJson<T>(File.ReadAllText(source)) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Save(T value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            File.WriteAllText(_temporaryPath, JsonUtility.ToJson(value));
            if (File.Exists(_path))
            {
                File.Replace(_temporaryPath, _path, null);
            }
            else
            {
                File.Move(_temporaryPath, _path);
            }
        }

        public void Clear()
        {
            foreach (var path in new[] { _path, _temporaryPath })
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}
