using System;
using GameUtilities.Utils;
using System.Collections.Generic;
using Town;
using UnityEngine;
using System.Collections;
using System.IO;

public class UnitManager
{
    public static bool IsLoadedCaches { get; set; } = false;

    private static GameObject _unitFolder = null;

    private static Dictionary<TownItem, GameObject> _townUnitFolder = new();
    private static Dictionary<string, Sprite> _spriteCache = new();
    private static Dictionary<string, List<Sprite>> _animationCache = new();
    private static Dictionary<UnitItem, Coroutine> _activeAnimations = new();

    private static float _unitSize => MapConfig.CellSize * 3;

    public static void InitializeCaches()
    {
        IsLoadedCaches = true;
        _spriteCache.Clear();
        _animationCache.Clear();

        foreach (var unitGroup in UnitsTexturesConfig.UnitsGroupsList)
        {
            foreach (var actionGroup in unitGroup.ActionGroupsList)
            {
                var animKey = $"{unitGroup.UnitId}_{actionGroup.ActionName}";
                var sprites = new List<Sprite>();

                foreach (var textureInfo in actionGroup.UnitTexturesList)
                {
                    var texturePath = $"{UnitsTexturesConfig.TexturesPath}{unitGroup.UnitFolder}{actionGroup.ActionFolder}{textureInfo.TextureFileName}";

                    if (_spriteCache.TryGetValue(texturePath, out var existingSprite))
                    {
                        sprites.Add(existingSprite);
                    }
                    else
                    {
                        var fileData = File.ReadAllBytes(texturePath);
                        var texture = new Texture2D(2, 2);
                        if (!texture.LoadImage(fileData))
                        {
                            Debug.LogWarning($"Failed to load sprite at path: {texturePath}");
                        }
                        else
                        {
                            texture.filterMode = FilterMode.Point;
                            texture.wrapMode = TextureWrapMode.Clamp;
                            texture.Apply();

                            var sprite = Sprite.Create(
                                texture,
                                new Rect(0.0f, 0.0f, texture.width, texture.height),
                                Vector2.zero
                            );
                            _spriteCache[texturePath] = sprite;
                            sprites.Add(sprite);
                        }
                    }
                }

                if (sprites.Count > 0)
                {
                    _animationCache[animKey] = sprites;
                    Debug.Log($"Cached animation: {animKey} with {sprites.Count} frames");
                }
            }
        }

        Debug.Log($"Sprite cache initialized with {_spriteCache.Count} sprites and {_animationCache.Count} animations");
    }

    public static UnitItem CreateUnit(Vector2 position, int unitId, TownItem town)
    {
        if (_spriteCache.Count == 0)
        {
            InitializeCaches();
        }

        if (_unitFolder == null)
        {
            _unitFolder = new GameObject("Units");
        }

        if (!_townUnitFolder.ContainsKey(town))
        {
            var townFolder = new GameObject($"{town.Name}_Folder");
            townFolder.transform.parent = _unitFolder.transform;
            _townUnitFolder.Add(town, townFolder);
        }

        var unitName = UtilsClass.GetRandomName();
        var unitGameObject = new GameObject($"{unitName}_{town.Name}_Unit");
        unitGameObject.transform.parent = _townUnitFolder[town].transform;
        unitGameObject.transform.position = new Vector3(position.x, position.y, 0);
        unitGameObject.transform.localScale = new Vector3(_unitSize, _unitSize, 1f);

        var unit = UnitsConfig.Units.Find(x => x.Id == unitId);
        var spriteRenderer = unitGameObject.AddComponent<SpriteRenderer>();

        SetupUnitVisuals(unit, spriteRenderer);

        var unitItem = UnitItem.Create(position, Guid.NewGuid(), UtilsClass.GetRandomName(), unit, unitGameObject, town);

        PlayAnimation(unitItem, "Idle");
        
        town.AddUnit(unitItem);

        return unitItem;
    }

    private static void SetupUnitVisuals(Unit unit, SpriteRenderer render)
    {
        var animKey = $"{unit.Id}_Idle";

        if (_animationCache.TryGetValue(animKey, out var idleFrames) && idleFrames.Count > 0)
        {
            render.sprite = idleFrames[0];
        }
        else
        {
            Debug.LogWarning($"No Idle animation found for unit type {unit.Id}");
        }
    }

    private static void PlayAnimation(UnitItem unit, string actionName, bool loop = true, float frameRate = 3f)
    {
        var animKey = $"{unit.Unit.Id}_{actionName}";
        var renderer = unit.SpriteRenderer;

        if (!_animationCache.TryGetValue(animKey, out var frames))
        {
            Debug.LogWarning($"Animation {animKey} not found in cache!");
            return;
        }

        StopAnimation(unit);

        ApplySpriteRotation(unit);

        if (unit.UnitGameObject != null)
        {
            var monoBehaviour = unit.UnitGameObject.GetComponent<MonoBehaviour>();

            var coroutine = monoBehaviour.StartCoroutine(PlaySpriteAnimation(renderer, frames, frameRate, loop));
            _activeAnimations[unit] = coroutine;
        }
    }

    private static void ApplySpriteRotation(UnitItem unit)
    {
        if (unit?.UnitGameObject == null || unit.SpriteRenderer == null)
            return;

        if (unit.UnitMoveDirection == Vector2.zero)
            return;

        var scale = unit.UnitGameObject.transform.localScale;

        var absScaleX = Mathf.Abs(scale.x);

        unit.UnitGameObject.transform.localScale = new Vector3(
            unit.UnitMoveDirection.x < 0 ? -absScaleX : absScaleX,
            scale.y,
            scale.z
        );

        unit.DisplayGroupCounter.RotateText(unit.UnitMoveDirection);
    }

    private static IEnumerator PlaySpriteAnimation(SpriteRenderer renderer, List<Sprite> sprites, float frameRate = 3f, bool loop = true)
    {
        if (sprites == null || sprites.Count == 0)
            yield break;

        var secondsPerFrame = 1f / frameRate;
        var wait = new WaitForSeconds(secondsPerFrame);
        var totalFrames = sprites.Count;

        var currentFrame = 0;

        do
        {
            if (renderer != null)
            {
                renderer.sprite = sprites[currentFrame];
                yield return wait;

                currentFrame = (currentFrame + 1) % totalFrames;
            }
            else
            {
                yield break;
            }
        } while (loop || currentFrame != 0);
    }

    private static void StopAnimation(UnitItem unit)
    {
        if (_activeAnimations.TryGetValue(unit, out var coroutine) && unit.UnitGameObject != null)
        {
            var monoBehaviour = unit.UnitGameObject.GetComponent<MonoBehaviour>();
            if (monoBehaviour != null)
            {
                monoBehaviour.StopCoroutine(coroutine);
            }
            _activeAnimations.Remove(unit);
        }
    }

    public static void UpdateAnimation(UnitItem unit, bool loop = true, float frameRate = 3f)
    {
        switch (unit.State)
        {
            case UnitState.Move:
                PlayAnimation(unit, "Move", loop, frameRate);
                break;
            case UnitState.Attacking:
                PlayAnimation(unit, "Attack", loop, frameRate);
                break;
            case UnitState.Idle:
                PlayAnimation(unit, "Idle", loop, frameRate);
                break;
            case UnitState.Dying:
                PlayAnimation(unit, "Die", loop, frameRate);
                break;
            case UnitState.TakingDamage:
                PlayAnimation(unit, "TakeDamage", loop, frameRate);
                break;
        }
    }
}

public enum UnitState
{
    Idle,
    Move,
    Attacking,
    Dying,
    TakingDamage
}
