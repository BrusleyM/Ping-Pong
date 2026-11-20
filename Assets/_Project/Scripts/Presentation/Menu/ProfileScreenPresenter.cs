using System;
using PingPong.Core;
using PingPong.Gameplay;
using TMPro;
using UnityEngine;

namespace PingPong.Presentation
{
    public sealed class ProfileScreenPresenter : MonoBehaviour
    {
        [SerializeField] private NicknameKeyboard _keyboard;
        [SerializeField] private TMP_Text _nickname;
        [SerializeField] private TMP_Text _hint;
        [SerializeField] private PopButton _red;
        [SerializeField] private PopButton _blue;
        [SerializeField] private GameObject _redSelected;
        [SerializeField] private GameObject _blueSelected;
        [SerializeField] private PopButton _save;
        [SerializeField] private TMP_Text _saveLabel;
        [SerializeField] private TMP_Text _heading;
        [SerializeField] private CharacterPreview _preview;
        [SerializeField] private GameObject _cancelButton;
        [SerializeField] private Camera _previewCamera;

        private TeamColour _team = TeamColour.Red;

        public event Action Saved;

        private void Awake()
        {
            _keyboard.Changed += OnNameChanged;
            _red.Activated += () => Choose(TeamColour.Red);
            _blue.Activated += () => Choose(TeamColour.Blue);
            _save.Activated += OnSave;
            _save.Refused += () => _hint.text = "Pick a nickname";
        }

        private void OnEnable()
        {
            _previewCamera.enabled = true;
        }

        private void OnDisable()
        {
            _previewCamera.enabled = false;
        }

        public void Open()
        {
            var existing = AppServices.Profile.Current;
            var editing = existing != null;
            _team = editing ? existing.Team : TeamColour.Red;
            _keyboard.SetText(editing ? existing.Nickname : string.Empty);
            _heading.text = editing ? "Edit your player" : "Create your player";
            _saveLabel.text = editing ? "Save changes" : "Let's go!";
            _cancelButton.SetActive(editing);
            Choose(_team);
        }

        private void Choose(TeamColour team)
        {
            _team = team;
            _redSelected.SetActive(team == TeamColour.Red);
            _blueSelected.SetActive(team == TeamColour.Blue);
            _preview.Show(team);
        }

        private void OnNameChanged(string text)
        {
            var valid = NicknameRules.IsValid(text);
            _nickname.text = valid ? text : "Type your nickname";
            _save.Locked = !valid;
            _hint.text = valid ? string.Empty : "Pick a nickname";
        }

        private void OnSave()
        {
            if (AppServices.Profile.TrySave(_keyboard.Text, _team))
            {
                Saved?.Invoke();
            }
        }
    }
}
