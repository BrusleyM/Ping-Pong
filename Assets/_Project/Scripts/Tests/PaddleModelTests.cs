using NUnit.Framework;
using PingPong.Gameplay;
using UnityEditor;
using UnityEngine;

namespace PingPong.Tests
{
    public sealed class PaddleModelTests
    {
        GameObject _root;

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        /// <summary>A paddle whose blade sits on a child at an arbitrary angle, with arbitrary model axes.</summary>
        PaddleModel Build(Quaternion bladeInRoot, Vector3 faceNormalLocal, Vector3 handleToBladeLocal)
        {
            _root = new GameObject("Paddle");
            var blade = new GameObject("Blade").transform;
            blade.SetParent(_root.transform, false);
            blade.localPosition = new Vector3(0.01f, 0.02f, 0.15f);
            blade.localRotation = bladeInRoot;
            var model = _root.AddComponent<PaddleModel>();
            var so = new SerializedObject(model);
            so.FindProperty("_blade").objectReferenceValue = blade;
            so.FindProperty("_faceNormalLocal").vector3Value = faceNormalLocal;
            so.FindProperty("_handleToBladeLocal").vector3Value = handleToBladeLocal;
            so.ApplyModifiedPropertiesWithoutUndo();
            return model;
        }

        [TestCase(281.85f, 4.63f, 137.43f, 0f, 0f, 1f, -0.57f, 0.82f, 0f)] // the project's paddle
        [TestCase(0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f)]                     // face up, handle along -Z
        [TestCase(30f, -70f, 15f, 1f, 0f, 0f, 0f, 0.3f, -1f)]              // odd axes; up not perpendicular
        public void RootRotationFor_TurnsTheFaceAndKeepsTheBladeUp(float bx, float by, float bz,
            float nx, float ny, float nz, float ux, float uy, float uz)
        {
            var model = Build(Quaternion.Euler(bx, by, bz), new Vector3(nx, ny, nz), new Vector3(ux, uy, uz));
            Vector3 face = new Vector3(-1f, 0f, 0.2f).normalized;

            model.transform.rotation = model.RootRotationFor(face, Vector3.up);

            Vector3 faceWorld = model.Blade.TransformDirection(model.FaceNormalLocal);
            Assert.That(Vector3.Angle(faceWorld, face), Is.LessThan(0.5f), "blade face points where asked");
            Vector3 upWorld = model.Blade.TransformDirection(model.HandleToBladeLocal);
            Vector3 wantedUp = Vector3.ProjectOnPlane(Vector3.up, face).normalized;
            Vector3 upInPlane = Vector3.ProjectOnPlane(upWorld, faceWorld).normalized;
            Assert.That(Vector3.Dot(upInPlane, wantedUp), Is.GreaterThan(0.99f), "blade above the handle");
        }

        [Test]
        public void RootPoseFor_PutsTheBladeCentreAndFaceWhereAsked()
        {
            var model = Build(Quaternion.Euler(281.85f, 4.63f, 137.43f), Vector3.forward, new Vector3(-0.57f, 0.82f, 0f));
            model.transform.SetPositionAndRotation(new Vector3(3f, 1f, -2f), Quaternion.Euler(10f, 50f, -20f));
            Vector3 centre = new Vector3(0.4f, 1.2f, -1.6f);
            Vector3 face = new Vector3(0.3f, 0.1f, 1f).normalized;

            Pose pose = model.RootPoseFor(centre, face, Vector3.up);
            model.transform.SetPositionAndRotation(pose.position, pose.rotation);

            Assert.That(Vector3.Distance(model.Blade.position, centre), Is.LessThan(0.001f), "blade centre where asked");
            Assert.That(Vector3.Angle(model.Blade.TransformDirection(model.FaceNormalLocal), face), Is.LessThan(0.5f));
        }
    }
}
