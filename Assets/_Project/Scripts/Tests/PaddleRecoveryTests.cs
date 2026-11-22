using NUnit.Framework;
using PingPong.Core;
using PingPong.Gameplay;
using UnityEditor;
using UnityEngine;

namespace PingPong.Tests
{
    public sealed class PaddleRecoveryTests
    {
        GameObject _paddle, _table, _floor, _tableLeg;
        PaddleRecovery _recovery;
        PaddleRecoveryProfile _profile;
        Rigidbody _body;

        [SetUp]
        public void SetUp()
        {
            _table = new GameObject("Table");
            var top = new GameObject("Top");
            top.transform.SetParent(_table.transform, false);
            var box = top.AddComponent<BoxCollider>();
            box.size = new Vector3(1.525f, 0.05f, 2.74f);
            top.transform.position = new Vector3(0f, 0.735f, 0f);
            var tableComponent = top.AddComponent<PingPongTable>();
            SetObject(tableComponent, "_top", box);
            _tableLeg = new GameObject("Leg", typeof(BoxCollider));
            _tableLeg.transform.SetParent(_table.transform, false);
            _floor = new GameObject("Floor", typeof(BoxCollider));

            _paddle = new GameObject("Paddle", typeof(Rigidbody));
            _paddle.SetActive(false);
            var blade = new GameObject("Blade").transform;
            blade.SetParent(_paddle.transform, false);
            blade.localPosition = new Vector3(0f, 0.15f, 0f);
            var model = _paddle.AddComponent<PaddleModel>();
            SetObject(model, "_blade", blade);
            var grip = _paddle.AddComponent<PaddleGrip>();
            _profile = ScriptableObject.CreateInstance<PaddleRecoveryProfile>();
            _recovery = _paddle.AddComponent<PaddleRecovery>();
            SetObject(_recovery, "_profile", _profile);
            SetObject(_recovery, "_table", tableComponent);
            SetObject(_recovery, "_tableRoot", _table.transform);
            SetObject(_recovery, "_model", model);
            SetObject(_recovery, "_grip", grip);
            _paddle.SetActive(true);
            typeof(PaddleRecovery).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(_recovery, null);
            _body = _paddle.GetComponent<Rigidbody>();
            _paddle.transform.position = new Vector3(2f, 0.05f, 3f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_paddle);
            Object.DestroyImmediate(_table);
            Object.DestroyImmediate(_floor);
            Object.DestroyImmediate(_profile);
        }

        static void SetObject(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        void RunFor(float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.02f) _recovery.Tick(0.02f);
        }

        [Test]
        public void TouchingTheFloor_StartsTheRecovery_WithoutPerFrameWorkBeforeThat()
        {
            Assert.That(_recovery.enabled, Is.False, "idle: no per-frame work");

            _recovery.NoticeContact(_floor.GetComponent<Collider>());

            Assert.That(_recovery.IsRecovering, Is.True);
            Assert.That(_recovery.enabled, Is.True);
        }

        [Test]
        public void TouchingTheTable_DoesNotStartTheRecovery()
        {
            _recovery.NoticeContact(_tableLeg.GetComponent<Collider>());

            Assert.That(_recovery.IsRecovering, Is.False);
        }

        [Test]
        public void APaddleSomeoneHolds_IsNotRecovered()
        {
            _body.isKinematic = true;

            _recovery.NoticeContact(_floor.GetComponent<Collider>());

            Assert.That(_recovery.IsRecovering, Is.False);
        }

        [Test]
        public void ThePaddleStaysWhereItFellUntilTheSettleTimeIsUp()
        {
            Vector3 fell = _body.position;
            _recovery.NoticeContact(_floor.GetComponent<Collider>());

            RunFor(_profile.Feel.SettleSeconds - 0.1f);

            Assert.That(_body.position, Is.EqualTo(fell).Using(NearlyEqual.Instance));
            Assert.That(_body.useGravity, Is.True);
        }

        [Test]
        public void WhileGliding_GravityIsOff_SoThePaddleFliesInsteadOfFalling()
        {
            _recovery.NoticeContact(_floor.GetComponent<Collider>());

            RunFor(_profile.Feel.SettleSeconds + 0.2f);

            Assert.That(_body.useGravity, Is.False);
            Assert.That(_recovery.IsRecovering, Is.True);
        }

        [Test]
        public void AfterTheGlide_ThePaddleIsOverThePlayersEndOfTheTable_AndFallsNormallyAgain()
        {
            _recovery.NoticeContact(_floor.GetComponent<Collider>());

            RunFor(_profile.Feel.SettleSeconds + _profile.Feel.GlideSeconds + 0.2f);

            Assert.That(_recovery.IsRecovering, Is.False);
            Assert.That(_recovery.enabled, Is.False, "idle again");
            Assert.That(_body.useGravity, Is.True);
            Assert.That(_body.position.z, Is.LessThan(0f), "the player's end");
            Assert.That(_body.position.y, Is.GreaterThan(0.76f), "above the table surface");
            Assert.That(Mathf.Abs(_body.position.x), Is.LessThan(0.8f), "over the table, not past its side");
        }

        [Test]
        public void GrabbingThePaddleMidGlide_StopsTheGlide_AndGivesGravityBack()
        {
            _recovery.NoticeContact(_floor.GetComponent<Collider>());
            RunFor(_profile.Feel.SettleSeconds + 0.3f);

            _body.isKinematic = true;
            RunFor(0.1f);

            Assert.That(_recovery.IsRecovering, Is.False);
            Assert.That(_body.useGravity, Is.True);
        }

        [Test]
        public void ASecondContactWhileRecovering_DoesNotRestartTheWait()
        {
            _recovery.NoticeContact(_floor.GetComponent<Collider>());
            RunFor(0.5f);

            _recovery.NoticeContact(_floor.GetComponent<Collider>());
            RunFor(0.2f);

            Assert.That(_body.useGravity, Is.False, "the glide began on the first contact's schedule");
        }
    }
}
