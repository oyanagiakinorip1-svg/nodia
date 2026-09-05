using System.Collections.Generic;
using UnityEngine;
using Nodia.Data;
using Nodia.Networking;

namespace Nodia.Nodes
{
    // Shift+click a node to start a link, then Shift+click a different node
    // to complete it - a two-click flow using the node's own (large, easy to
    // aim at) collider, rather than a separate small anchor sphere that was
    // too fiddly to hit once nodes got bigger. To remove a link, Shift+click
    // the line itself directly (it has its own capsule collider) - re-picking
    // both endpoints again was too much of a detour.
    public class ConnectionManager : MonoBehaviour
    {
        [SerializeField] private GameObject linePrefab; // must have a LineRenderer
        [SerializeField] private NodeManager nodeManager;
        [SerializeField] private float lineColliderRadius = 0.15f;

        private NodeView pendingFromNode;
        private readonly Dictionary<(string, string), (string id, GameObject line)> connectionsByPair = new();

        public void HandleNodeClicked(NodeView node)
        {
            if (pendingFromNode == null)
            {
                pendingFromNode = node;
                node.SetPendingConnection(true);
                return;
            }

            if (pendingFromNode != node)
            {
                var key = PairKey(pendingFromNode.Data.id, node.Data.id);
                if (connectionsByPair.TryGetValue(key, out var existing))
                {
                    RemoveConnection(key, existing);
                }
                else
                {
                    CreateConnection(pendingFromNode, node);
                }
            }

            pendingFromNode.SetPendingConnection(false);
            pendingFromNode = null;
        }

        // Shift+clicking a line directly - no need to re-select its endpoints.
        public void HandleLineClicked(ConnectionLineView line)
        {
            if (connectionsByPair.TryGetValue(line.PairKey, out var entry) && entry.line == line.gameObject)
            {
                RemoveConnection(line.PairKey, entry);
            }
        }

        // Same optimistic pattern as node create/delete: draw the line right
        // away and adopt the server-assigned id once the save completes,
        // instead of waiting on the round trip to show anything.
        private void CreateConnection(NodeView from, NodeView to)
        {
            var key = PairKey(from.Data.id, to.Data.id);
            var lineObj = CreateLineVisual(from.transform, to.transform, key);
            connectionsByPair[key] = (null, lineObj);

            ApiClient.Instance.CreateConnection(from.Data.id, to.Data.id, nodeManager.CurrentSpaceId, data =>
            {
                if (lineObj == null) return; // deleted again before the save finished
                lineObj.name = $"connection_{data.id}";
                lineObj.GetComponent<ConnectionLineView>().ConnectionId = data.id;
                connectionsByPair[key] = (data.id, lineObj);
            }, err =>
            {
                Debug.LogError(err);
                connectionsByPair.Remove(key);
                if (lineObj != null) Destroy(lineObj);
            });
        }

        private void RemoveConnection((string, string) key, (string id, GameObject line) entry)
        {
            connectionsByPair.Remove(key);
            if (entry.line != null) Destroy(entry.line);
            // Null id means the create request for this line hadn't come back
            // yet - nothing exists server-side to delete.
            if (entry.id != null)
            {
                ApiClient.Instance.DeleteConnection(entry.id, () => { }, Debug.LogError);
            }
        }

        public void SpawnExistingConnection(ConnectionData data, NodeView from, NodeView to)
        {
            var key = PairKey(from.Data.id, to.Data.id);
            var lineObj = CreateLineVisual(from.transform, to.transform, key);
            lineObj.name = $"connection_{data.id}";
            lineObj.GetComponent<ConnectionLineView>().ConnectionId = data.id;
            connectionsByPair[key] = (data.id, lineObj);
        }

        private GameObject CreateLineVisual(Transform from, Transform to, (string, string) pairKey)
        {
            var lineObj = Instantiate(linePrefab, transform);
            var line = lineObj.GetComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, from.position);
            line.SetPosition(1, to.position);

            var view = lineObj.AddComponent<ConnectionLineView>();
            view.PairKey = pairKey;
            AddLineCollider(lineObj, from.position, to.position);

            return lineObj;
        }

        // Called when switching spaces: the previous space's lines (and any
        // half-made connection pointing at its now-destroyed nodes) all need
        // to go before the new space's nodes load in.
        public void ClearConnections()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            connectionsByPair.Clear();
            pendingFromNode = null;
        }

        // LineRenderer has no collision of its own, so a capsule spanning the
        // two endpoints (thicker than the visual line, matching the "big,
        // easy-to-aim-at" collider philosophy used everywhere else here)
        // is what actually makes the line clickable.
        private void AddLineCollider(GameObject lineObj, Vector3 from, Vector3 to)
        {
            var colliderGO = new GameObject("Collider", typeof(CapsuleCollider));
            colliderGO.transform.SetParent(lineObj.transform, false);
            colliderGO.transform.position = (from + to) * 0.5f;
            colliderGO.transform.rotation = Quaternion.LookRotation((to - from).normalized);

            var capsule = colliderGO.GetComponent<CapsuleCollider>();
            capsule.direction = 2; // local Z, matching the rotation above
            capsule.radius = lineColliderRadius;
            capsule.height = Vector3.Distance(from, to);
        }

        private static (string, string) PairKey(string a, string b)
            => string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
    }
}
