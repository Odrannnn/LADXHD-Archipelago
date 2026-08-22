using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace ProjectZ.Base.UI
{
    public class UiManager
    {
        public static bool HideOverlay = false;

        public string CurrentScreen
        {
            get => _currentScreen;
            set => _currentScreen = value.ToUpper();
        }

        private readonly List<UiElement> _elementList = new List<UiElement>();
        private readonly List<UiElement> _elementListNoHide = new List<UiElement>();

        private string _currentScreen;

        private IEnumerable<UiElement> AllElements => _elementList.Concat(_elementListNoHide);

        private static void RemoveMarked(List<UiElement> list) =>
            list.RemoveAll(e => e.Remove);

        private void UpdateElements(List<UiElement> list)
        {
            foreach (var element in list.ToList())
                if (element.Screens.Contains(_currentScreen))
                    element.Update();
        }

        public void Update()
        {
            RemoveMarked(_elementList);
            RemoveMarked(_elementListNoHide);
            UpdateElements(_elementList);
            UpdateElements(_elementListNoHide);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (HideOverlay)
            {
                // At least draw the dialog box.
                Game1.GameManager.InGameOverlay.TextboxOverlay.TextboxBackground.Draw(spriteBatch);

                // And draw dialog choices.
                foreach (var element in Game1.GameManager.InGameOverlay.TextboxOverlay.TextboxBackgroundChoice.ToList())
                    if (element.Screens.Contains(_currentScreen) && element.IsVisible)
                        element.Draw(spriteBatch);

                return;
            } 

            foreach (var element in AllElements.ToList())
                if (element.Screens.Contains(_currentScreen) && element.IsVisible)
                    element.Draw(spriteBatch);
        }

        public void DrawBlur(SpriteBatch spriteBatch)
        {
            foreach (var element in _elementListNoHide.ToList())
                if (element.Screens.Contains(_currentScreen) && element.IsVisible)
                    element.DrawBlur(spriteBatch);

            if (HideOverlay) return;

            foreach (var element in _elementList.ToList())
                if (element.Screens.Contains(_currentScreen) && element.IsVisible)
                    element.DrawBlur(spriteBatch);
        }

        public void OnResize()
        {
            foreach (var uiElement in AllElements.ToList())
                uiElement.SizeUpdate?.Invoke(uiElement);
        }

        public UiElement AddElement(UiElement element, bool alwaysDraw = false)
        {
            if (element == null) return null;

            if (alwaysDraw)
                _elementListNoHide.Add(element);
            else
                _elementList.Add(element);

            return element;
        }

        public UiElement GetElement(string elementId)
        {
            return AllElements.FirstOrDefault(e => e.ElementId == elementId);
        }

        public void RemoveElement(string elementId, string screenId)
        {
            foreach (var element in AllElements.ToList())
                if (element.ElementId.Contains(elementId) && element.Screens.Contains(screenId))
                    element.Remove = true;
        }
    }
}
