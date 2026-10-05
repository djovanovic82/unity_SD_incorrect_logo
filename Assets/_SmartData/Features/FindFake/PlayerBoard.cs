using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SmartData.FindFake
{
    /// <summary>
    /// Tabla jednog igrača. Polja postoje u sceni (Setup/Repair ih kreira), runtime
    /// samo dodeljuje sprite-ove. Raspored polja se računa iz stvarne veličine zone,
    /// pa radi na svakom display profilu bez posebne scene.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class PlayerBoard : MonoBehaviour
    {
        public RectTransform tilesRoot;
        public List<LogoTile> tiles = new List<LogoTile>();
        public Image lockOverlay;
        public TMP_Text lockLabel;

        [SerializeField, HideInInspector] private int tileCount = 16;
        [SerializeField, HideInInspector] private int columns = 4;
        [SerializeField, HideInInspector] private int rows = 4;
        [SerializeField, HideInInspector] private bool autoShape = true;
        [SerializeField, HideInInspector] private float padding = 16f;
        [SerializeField, HideInInspector] private float spacing = 18f;
        [SerializeField, HideInInspector] private float maxTile = 320f;

        private Action<PlayerBoard, LogoTile, PointerEventData> onTileDown;
        private bool locked = true;
        private float frozenUntil = -1f;
        private string frozenText = "";
        private int fakeIndex = -1;

        public int PlayerIndex { get; private set; } = 1;
        public int TileCount => tileCount;
        public int FakeIndex => fakeIndex;
        public Vector2Int CurrentShape { get; private set; }
        public bool IsFrozen => frozenUntil > 0f && Time.unscaledTime < frozenUntil;
        public bool AcceptsInput => !locked && !IsFrozen;

        public void Bind(int playerIndex, Action<PlayerBoard, LogoTile, PointerEventData> callback)
        {
            PlayerIndex = playerIndex;
            onTileDown = callback;
        }

        /// <summary>Primenjuje mrežu i vraća eventualno novokreirana polja (Editor ih registruje za Undo).</summary>
        public List<LogoTile> Configure(GameplayBlock gameplay, LayoutBlock layout)
        {
            tileCount = gameplay.TileCount;
            columns = Mathf.Max(1, gameplay.columns);
            rows = Mathf.Max(1, gameplay.rows);
            autoShape = gameplay.autoGridShape;
            padding = layout.boardPadding;
            spacing = layout.tileSpacing;
            maxTile = layout.maxTileSize;
            List<LogoTile> created = EnsureTiles();
            LayoutTiles();
            return created;
        }

        /// <summary>
        /// Obezbeđuje dovoljno polja kloniranjem prvog. Višak se samo deaktivira
        /// (nikad se ne briše – čuva ručne izmene). Vraća novokreirana polja.
        /// </summary>
        public List<LogoTile> EnsureTiles()
        {
            var created = new List<LogoTile>();
            tiles.RemoveAll(t => t == null);
            if (tiles.Count == 0 || tilesRoot == null) return created;

            while (tiles.Count < tileCount)
            {
                LogoTile clone = Instantiate(tiles[0], tilesRoot);
                clone.name = "Tile_" + tiles.Count.ToString("00");
                tiles.Add(clone);
                created.Add(clone);
            }

            for (int i = 0; i < tiles.Count; i++)
            {
                bool shouldBeActive = i < tileCount;
                if (tiles[i].gameObject.activeSelf != shouldBeActive) tiles[i].gameObject.SetActive(shouldBeActive);
                tiles[i].Setup(this, i);
            }
            return created;
        }

        public void ApplyVisuals(VisualBlock visuals, TypographyBlock typography, float fontScale)
        {
            foreach (LogoTile tile in tiles)
            {
                if (tile != null) tile.ApplyVisuals(visuals);
            }
            if (lockOverlay != null)
            {
                lockOverlay.color = visuals.lockOverlayColor;
                lockOverlay.raycastTarget = true; // blokira dodire dok je tabla zamrznuta
            }
            UiStyle.Text(lockLabel, typography, typography.lockSize, fontScale, visuals.textColor);
        }

        public void LayoutTiles()
        {
            if (tilesRoot == null) return;
            Vector2 size = tilesRoot.rect.size;
            int count = Mathf.Min(tileCount, tiles.Count);
            if (count <= 0 || size.x <= 1f || size.y <= 1f) return;

            Vector2 avail = size - new Vector2(padding * 2f, padding * 2f);
            Vector2Int shape = autoShape ? BestShape(count, avail) : new Vector2Int(columns, Mathf.CeilToInt(count / (float)columns));
            CurrentShape = shape;
            float cell = CellSize(shape, avail);

            int c = shape.x;
            int r = shape.y;
            float gridW = c * cell + (c - 1) * spacing;
            float gridH = r * cell + (r - 1) * spacing;
            int lastRowItems = count - (r - 1) * c;
            var center = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < count; i++)
            {
                LogoTile tile = tiles[i];
                if (tile == null) continue;
                int row = i / c;
                int col = i % c;
                float x = -gridW * 0.5f + cell * 0.5f + col * (cell + spacing);
                if (row == r - 1 && lastRowItems < c) x += (c - lastRowItems) * (cell + spacing) * 0.5f; // poslednji red centriran
                float y = gridH * 0.5f - cell * 0.5f - row * (cell + spacing);

                RectTransform rt = tile.Rect;
                if (rt.anchorMin != center) rt.anchorMin = center;
                if (rt.anchorMax != center) rt.anchorMax = center;
                if (rt.pivot != center) rt.pivot = center;
                var pos = new Vector2(x, y);
                var sz = new Vector2(cell, cell);
                if ((rt.anchoredPosition - pos).sqrMagnitude > 0.01f) rt.anchoredPosition = pos;
                if ((rt.sizeDelta - sz).sqrMagnitude > 0.01f) rt.sizeDelta = sz;
            }
        }

        /// <summary>
        /// Bira raspored (kolone × redovi) za isti broj polja tako da polja budu najveća.
        /// Nepotpun poslednji red se prihvata samo ako je dobitak veći od ~15%.
        /// </summary>
        private Vector2Int BestShape(int count, Vector2 avail)
        {
            var best = new Vector2Int(columns, Mathf.CeilToInt(count / (float)columns));
            float bestScore = Score(best, count, avail);
            for (int c = 1; c <= count; c++)
            {
                int r = Mathf.CeilToInt(count / (float)c);
                if ((r - 1) * c >= count) continue; // prazan red
                var shape = new Vector2Int(c, r);
                float score = Score(shape, count, avail);
                bool better = score > bestScore + 0.5f;
                bool tieMoreSquare = Mathf.Abs(score - bestScore) <= 0.5f && Mathf.Abs(c - r) < Mathf.Abs(best.x - best.y);
                if (better || tieMoreSquare)
                {
                    best = shape;
                    bestScore = score;
                }
            }
            return best;
        }

        private float Score(Vector2Int shape, int count, Vector2 avail)
        {
            float cell = CellSize(shape, avail);
            return count % shape.x == 0 ? cell : cell * 0.85f;
        }

        private float CellSize(Vector2Int shape, Vector2 avail)
        {
            float w = (avail.x - (shape.x - 1) * spacing) / shape.x;
            float h = (avail.y - (shape.y - 1) * spacing) / shape.y;
            return Mathf.Clamp(Mathf.Min(w, h), 1f, maxTile);
        }

        private void OnEnable()
        {
            LayoutTiles();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled) LayoutTiles();
        }

        private void Update()
        {
            if (!Application.isPlaying || frozenUntil <= 0f) return;
            float remaining = frozenUntil - Time.unscaledTime;
            if (remaining <= 0f)
            {
                ClearFreeze();
                return;
            }
            if (lockLabel != null)
                UiStyle.SetText(lockLabel, frozenText + "\n" + remaining.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
        }

        // ---------- stanja ----------

        public void SetLocked(bool value)
        {
            locked = value;
        }

        public void Freeze(float duration, string label)
        {
            if (duration <= 0f) return;
            frozenUntil = Time.unscaledTime + duration;
            frozenText = label ?? "";
            if (lockOverlay != null) lockOverlay.gameObject.SetActive(true);
            UiStyle.SetText(lockLabel, frozenText);
        }

        public void ClearFreeze()
        {
            frozenUntil = -1f;
            if (lockOverlay != null && lockOverlay.gameObject.activeSelf) lockOverlay.gameObject.SetActive(false);
        }

        public void ShowBlank()
        {
            fakeIndex = -1;
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] != null) tiles[i].SetIcon(null);
            }
        }

        public void ShowRound(LogoData logo, int fakeSlot)
        {
            fakeIndex = fakeSlot;
            int count = Mathf.Min(tileCount, tiles.Count);
            for (int i = 0; i < count; i++)
            {
                LogoTile tile = tiles[i];
                if (tile == null) continue;
                tile.ResetVisual();
                tile.SetIcon(logo == null ? null : (i == fakeSlot ? logo.fakeSprite : logo.correctSprite));
            }
        }

        public void FlashWrong(int index, VisualBlock visuals, float duration)
        {
            LogoTile tile = Get(index);
            if (tile != null) tile.Flash(visuals.wrongColor, duration);
        }

        public void MarkCorrect(VisualBlock visuals, AnimationBlock animations)
        {
            LogoTile tile = Get(fakeIndex);
            if (tile != null) tile.Highlight(visuals.correctColor, animations.revealPulseScale, animations.revealPulseDuration);
        }

        public void RevealFake(VisualBlock visuals, AnimationBlock animations)
        {
            LogoTile tile = Get(fakeIndex);
            if (tile != null) tile.Highlight(visuals.revealColor, animations.revealPulseScale, animations.revealPulseDuration);
        }

        public void ResetBoard()
        {
            locked = true;
            ClearFreeze();
            fakeIndex = -1;
            foreach (LogoTile tile in tiles)
            {
                if (tile != null) tile.ResetVisual();
            }
        }

        public LogoTile Get(int index)
        {
            return index >= 0 && index < tiles.Count && index < tileCount ? tiles[index] : null;
        }

        internal void HandleTileDown(LogoTile tile, PointerEventData eventData)
        {
            if (!Application.isPlaying || !AcceptsInput) return;
            if (onTileDown != null) onTileDown(this, tile, eventData);
        }
    }
}
