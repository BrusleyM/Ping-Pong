using PingPong.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PingPong.Presentation
{
    public sealed class DifficultyScreenPresenter : MonoBehaviour
    {
        [SerializeField] private UiTheme _theme;
        [SerializeField] private AIProfile _ai;
        [SerializeField] private PopButton _template;
        [SerializeField] private Seam<ISceneLoader> _loader;
        [SerializeField] private GameObject _loading;

        private void OnValidate()
        {
            Validation.Seam(_loader, nameof(_loader), this);
        }

        private void Awake()
        {
            _template.gameObject.SetActive(false);
            for (var level = 0; level < _ai.LevelCount; level++)
            {
                var chosen = level;
                var style = _theme.DifficultyAt(level, _ai.Level(level).Name);
                var button = Instantiate(_template, _template.transform.parent);
                button.name = "Level " + style.Name;
                button.GetComponent<Image>().color = style.Colour;
                button.transform.Find("Name").GetComponent<TMP_Text>().text = style.Name;
                button.transform.Find("Note").GetComponent<TMP_Text>().text = style.Note;

                button.gameObject.SetActive(true);
                button.Activated += () => StartMatch(chosen);
            }
        }

        private void StartMatch(int level)
        {
            var loader = _loader.Value;
            if (loader.IsLoading)
            {
                return;
            }

            AppServices.PendingMatch = new MatchRequest(level, AppServices.Profile.Current.Team);
            _loading.SetActive(true);
            loader.Load(SceneNames.Game);
        }
    }
}
