using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmartData.FindFake
{
    /// <summary>
    /// Button koji poštuje centralni input lock, suzbija sintetički miš posle
    /// dodira i ima sopstveni debounce (sprečava dupli klik / dupli dodir).
    /// </summary>
    public class SmartButton : Button
    {
        private float lastClick = -999f;

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            FindFakeApp app = FindFakeApp.Instance;
            if (app != null && Application.isPlaying)
            {
                if (!app.Guard.AcceptUi(eventData, app.input))
                {
                    if (app.debug.logInput) Debug.Log("[FindFake] Klik ODBIJEN na '" + name + "': " + app.Guard.LastRejectReason);
                    return;
                }
                if (Time.unscaledTime - lastClick < app.input.buttonDebounce)
                {
                    if (app.debug.logInput) Debug.Log("[FindFake] Klik ODBIJEN na '" + name + "': buttonDebounce");
                    return;
                }
                if (app.debug.logInput) Debug.Log("[FindFake] Klik na '" + name + "' (pointerId " + eventData.pointerId + ", stanje " + app.State + ")");
            }

            lastClick = Time.unscaledTime;
            base.OnPointerClick(eventData);
        }
    }
}
