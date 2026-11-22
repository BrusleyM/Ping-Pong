using System.Collections.Generic;
using NUnit.Framework;
using PingPong.Core;

namespace PingPong.Tests
{
    public class MenuNavigatorTests
    {
        private MenuNavigator _navigator;
        private List<MenuScreen> _changes;

        [SetUp]
        public void SetUp()
        {
            _navigator = new MenuNavigator(MenuScreen.Main);
            _changes = new List<MenuScreen>();
            _navigator.Changed += _changes.Add;
        }

        [Test]
        public void The_navigator_starts_on_the_screen_it_is_given_and_cannot_go_back()
        {
            Assert.AreEqual(MenuScreen.Main, _navigator.Current);
            Assert.IsFalse(_navigator.CanGoBack);
            Assert.IsFalse(_navigator.Back());
            CollectionAssert.IsEmpty(_changes);
        }

        [Test]
        public void Going_forward_then_back_returns_through_the_same_screens()
        {
            _navigator.Go(MenuScreen.ChooseWhere);
            _navigator.Go(MenuScreen.ChooseWho);

            Assert.IsTrue(_navigator.Back());
            Assert.AreEqual(MenuScreen.ChooseWhere, _navigator.Current);
            Assert.IsTrue(_navigator.Back());
            Assert.AreEqual(MenuScreen.Main, _navigator.Current);
        }

        [Test]
        public void Settings_goes_back_to_whichever_screen_opened_it()
        {
            _navigator.Go(MenuScreen.Settings);
            _navigator.Back();
            _navigator.Go(MenuScreen.ChooseWhere);
            _navigator.Go(MenuScreen.Settings);
            _navigator.Back();

            Assert.AreEqual(MenuScreen.ChooseWhere, _navigator.Current);
        }

        [Test]
        public void Going_to_the_current_screen_changes_nothing()
        {
            _navigator.Go(MenuScreen.Main);

            CollectionAssert.IsEmpty(_changes);
            Assert.IsFalse(_navigator.CanGoBack);
        }

        [Test]
        public void Every_change_is_announced_with_the_new_current_screen()
        {
            _navigator.Go(MenuScreen.ChooseWhere);
            _navigator.Back();

            CollectionAssert.AreEqual(new[] { MenuScreen.ChooseWhere, MenuScreen.Main }, _changes);
        }

        [Test]
        public void Resetting_clears_the_trail()
        {
            _navigator.Go(MenuScreen.ChooseWhere);
            _navigator.Go(MenuScreen.ChooseWho);

            _navigator.ResetTo(MenuScreen.Main);

            Assert.AreEqual(MenuScreen.Main, _navigator.Current);
            Assert.IsFalse(_navigator.CanGoBack);
        }
    
        [Test]
        public void Entering_the_difficulty_screen_keeps_the_whole_way_back_to_the_main_menu()
        {
            var navigator = MenuNavigator.Entering(MenuScreen.ChooseDifficulty);

            Assert.AreEqual(MenuScreen.ChooseDifficulty, navigator.Current);
            navigator.Back();
            Assert.AreEqual(MenuScreen.ChooseWho, navigator.Current);
            navigator.Back();
            Assert.AreEqual(MenuScreen.ChooseWhere, navigator.Current);
            navigator.Back();
            Assert.AreEqual(MenuScreen.Main, navigator.Current);
            Assert.IsFalse(navigator.CanGoBack);
        }

        [Test]
        public void Entering_the_main_menu_starts_with_nothing_to_go_back_to()
        {
            var navigator = MenuNavigator.Entering(MenuScreen.Main);

            Assert.AreEqual(MenuScreen.Main, navigator.Current);
            Assert.IsFalse(navigator.CanGoBack);
        }
    }
}
