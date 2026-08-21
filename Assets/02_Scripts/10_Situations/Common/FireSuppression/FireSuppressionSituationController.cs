using System;
using System.Collections.Generic;
using UnityEngine;
using VirtualRescue.GameFlow;

namespace VirtualRescue.Situations.FireSuppression
{
    [DisallowMultipleComponent]
    public abstract class FireSuppressionSituationController : SituationController
    {
        private readonly List<FireObject> _activeFireObjects = new();
        private readonly HashSet<FireObject> _extinguishedFireObjects = new();
        private readonly Dictionary<FireObject, Action> _extinguishedHandlers = new();

        protected sealed override void OnActivated()
        {
            UnsubscribeFireEvents();
            _activeFireObjects.Clear();
            _extinguishedFireObjects.Clear();

            PrepareActiveFireObjects(_activeFireObjects);
            RemoveInvalidAndDuplicateFireObjects();

            if (_activeFireObjects.Count == 0)
            {
                Debug.LogError(
                    $"{name}: At least one active fire object must be assigned.",
                    this);
                return;
            }

            foreach (FireObject fireObject in _activeFireObjects)
            {
                FireObject subscribedFireObject = fireObject;
                Action handler = () => HandleFireExtinguished(subscribedFireObject);
                _extinguishedHandlers.Add(subscribedFireObject, handler);
                subscribedFireObject.OnExtinguished += handler;
            }

            OnFireSuppressionActivated();
        }

        protected sealed override void OnResolved()
        {
            OnFireSuppressionDeactivated();
            UnsubscribeFireEvents();
        }

        protected sealed override void OnFailed()
        {
            OnFireSuppressionDeactivated();
            UnsubscribeFireEvents();
        }

        protected sealed override void OnReset()
        {
            OnFireSuppressionDeactivated();
            UnsubscribeFireEvents();
            _activeFireObjects.Clear();
            _extinguishedFireObjects.Clear();
        }

        protected abstract void PrepareActiveFireObjects(List<FireObject> activeFireObjects);

        protected virtual void OnFireSuppressionActivated()
        {
        }

        protected virtual void OnFireSuppressionDeactivated()
        {
        }

        private void OnDisable()
        {
            OnFireSuppressionDeactivated();
            UnsubscribeFireEvents();
        }

        private void HandleFireExtinguished(FireObject fireObject)
        {
            if (!IsActive || fireObject == null)
            {
                return;
            }

            if (!_extinguishedFireObjects.Add(fireObject))
            {
                return;
            }

            if (_extinguishedFireObjects.Count < _activeFireObjects.Count)
            {
                return;
            }

            if (!ResolveSituation())
            {
                Debug.LogError($"{name}: The fire situation could not be resolved.", this);
            }
        }

        private void RemoveInvalidAndDuplicateFireObjects()
        {
            HashSet<FireObject> uniqueFireObjects = new();

            for (int index = _activeFireObjects.Count - 1; index >= 0; index--)
            {
                FireObject fireObject = _activeFireObjects[index];

                if (fireObject == null || !uniqueFireObjects.Add(fireObject))
                {
                    _activeFireObjects.RemoveAt(index);
                }
            }
        }

        private void UnsubscribeFireEvents()
        {
            foreach (KeyValuePair<FireObject, Action> entry in _extinguishedHandlers)
            {
                if (entry.Key != null)
                {
                    entry.Key.OnExtinguished -= entry.Value;
                }
            }

            _extinguishedHandlers.Clear();
        }
    }
}
