using System.Collections.Generic;
using NUnit.Framework;
using PingPong.Presentation;
using PingPong.Tutorial;
using UnityEditor;
using UnityEngine;

namespace PingPong.Tests
{
    public class TutorialSceneWiringTests
    {
        private const string WorldPath = "Assets/_Project/Prefabs/World.prefab";

        private GameObject _world;

        [SetUp]
        public void SetUp()
        {
            _world = PrefabUtility.LoadPrefabContents(WorldPath);
        }

        [TearDown]
        public void TearDown()
        {
            PrefabUtility.UnloadPrefabContents(_world);
        }

        private static List<string> UnassignedReferences(Object component)
        {
            var unassigned = new List<string>();
            var properties = new SerializedObject(component).GetIterator();
            while (properties.NextVisible(true))
            {
                if (properties.propertyType == SerializedPropertyType.ObjectReference
                    && properties.propertyPath != "m_Script"
                    && properties.objectReferenceValue == null)
                {
                    unassigned.Add(properties.propertyPath);
                }
            }

            return unassigned;
        }

        [Test]
        public void The_world_contains_exactly_one_tutorial_director()
        {
            Assert.AreEqual(1, _world.GetComponentsInChildren<TutorialDirector>(true).Length);
        }

        [Test]
        public void Every_reference_of_the_tutorial_director_in_the_world_is_assigned()
        {
            var director = _world.GetComponentInChildren<TutorialDirector>(true);

            CollectionAssert.IsEmpty(UnassignedReferences(director));
        }

        [Test]
        public void Every_reference_of_the_tutorial_prompt_view_in_the_world_is_assigned()
        {
            var view = _world.GetComponentInChildren<TutorialPromptView>(true);

            CollectionAssert.IsEmpty(UnassignedReferences(view));
        }

        [Test]
        public void The_mode_menu_offers_replaying_the_tutorial()
        {
            var menu = _world.GetComponentInChildren<ModeMenuPresenter>(true);

            Assert.IsNotNull(new SerializedObject(menu).FindProperty("_replayTutorial").objectReferenceValue);
        }

        [Test]
        public void The_mode_menu_does_not_open_by_itself_because_the_tutorial_opens_it_at_the_end()
        {
            var menu = _world.GetComponentInChildren<ModeMenuPresenter>(true);

            Assert.IsFalse(new SerializedObject(menu).FindProperty("_openOnStart").boolValue);
        }
    }
}
