using System.IO;
using NUnit.Framework;
using PingPong.Gameplay;

namespace PingPong.Tests
{
    public class JsonFileStoreTests
    {
        private string _directory;
        private JsonFileStore<PlayerProfile> _store;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "pingpong-store-" + System.Guid.NewGuid().ToString("N"));
            _store = new JsonFileStore<PlayerProfile>(_directory, "profile.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Test]
        public void Loading_before_anything_was_saved_gives_nothing()
        {
            Assert.IsNull(_store.Load());
        }

        [Test]
        public void A_saved_value_is_loaded_back_from_disk()
        {
            _store.Save(new PlayerProfile("Vishnu", TeamColour.Blue));

            var loaded = new JsonFileStore<PlayerProfile>(_directory, "profile.json").Load();

            Assert.AreEqual("Vishnu", loaded.Nickname);
            Assert.AreEqual(TeamColour.Blue, loaded.Team);
        }

        [Test]
        public void Saving_twice_keeps_the_latest_value()
        {
            _store.Save(new PlayerProfile("First", TeamColour.Red));
            _store.Save(new PlayerProfile("Second", TeamColour.Blue));

            Assert.AreEqual("Second", _store.Load().Nickname);
        }

        [Test]
        public void A_corrupt_file_loads_as_nothing_instead_of_throwing()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(Path.Combine(_directory, "profile.json"), "{ not json");

            Assert.IsNull(_store.Load());
        }

        [Test]
        public void Clearing_removes_the_saved_value()
        {
            _store.Save(new PlayerProfile("Vishnu", TeamColour.Red));

            _store.Clear();

            Assert.IsNull(_store.Load());
        }
    
        [Test]
        public void A_save_that_was_interrupted_before_the_final_move_is_still_loaded_from_the_temporary_file()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(Path.Combine(_directory, "profile.json.tmp"), UnityEngine.JsonUtility.ToJson(new PlayerProfile("Vishnu", TeamColour.Blue)));

            Assert.AreEqual("Vishnu", _store.Load().Nickname);
        }

        [Test]
        public void Clearing_also_removes_a_leftover_temporary_file()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(Path.Combine(_directory, "profile.json.tmp"), "{}");

            _store.Clear();

            Assert.IsFalse(File.Exists(Path.Combine(_directory, "profile.json.tmp")));
        }
    }
}
