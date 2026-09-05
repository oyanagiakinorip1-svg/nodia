using System.Collections.Generic;
using UnityEngine;
using Nodia.Data;
using Nodia.Networking;

namespace Nodia.Nodes
{
    // Owns every spawned node, loads the saved space on start, and mirrors
    // create/update/delete actions to the API.
    public class NodeManager : MonoBehaviour
    {
        [SerializeField] private GameObject nodePrefab;
        [SerializeField] private Transform nodesParent;
        [SerializeField] private ConnectionManager connectionManager;

        private readonly Dictionary<string, NodeView> nodesById = new();

        public string CurrentSpaceId { get; private set; }

        // Called by SpaceSelectController both for the first space a player
        // enters and any time they switch to a different one - loading isn't
        // automatic any more since auth and space choice both have to
        // resolve first.
        public void LoadSpace(string spaceId)
        {
            ClearCurrentSpace();
            CurrentSpaceId = spaceId;
            ApiClient.Instance.GetSpaceSnapshot(spaceId, space =>
            {
                foreach (var nodeData in space.nodes)
                {
                    SpawnNode(nodeData);
                }

                foreach (var connectionData in space.connections)
                {
                    if (nodesById.TryGetValue(connectionData.from_node, out var from) &&
                        nodesById.TryGetValue(connectionData.to_node, out var to))
                    {
                        connectionManager.SpawnExistingConnection(connectionData, from, to);
                    }
                }
            }, err => Debug.LogError(err));
        }

        public IEnumerable<NodeView> GetAllNodes() => nodesById.Values;

        // Called after deleting a space from the picker - if it's the one
        // currently loaded in the background, its nodes are now orphaned
        // rows that no longer exist server-side, so wipe them locally too.
        public void ClearIfCurrentSpace(string spaceId)
        {
            if (CurrentSpaceId == spaceId)
            {
                ClearCurrentSpace();
                CurrentSpaceId = null;
            }
        }

        private void ClearCurrentSpace()
        {
            foreach (var node in nodesById.Values)
            {
                if (node != null) Destroy(node.gameObject);
            }
            nodesById.Clear();
            connectionManager.ClearConnections();
        }

        public NodeView SpawnNode(NodeData data)
        {
            var go = Instantiate(nodePrefab, nodesParent);
            var view = go.GetComponent<NodeView>();
            view.Initialize(data);
            if (!string.IsNullOrEmpty(data.id))
            {
                nodesById[data.id] = view;
            }
            return view;
        }

        // Spawns the sphere immediately (before the server confirms) so
        // placing a node feels instant regardless of network latency, then
        // adopts the server-assigned id once the save actually completes.
        public void CreateNodeAt(Vector3 position)
        {
            var placeholder = new NodeData
            {
                title = "無題のノート",
                content = "",
                position_x = position.x,
                position_y = position.y,
                position_z = position.z,
            };
            var view = SpawnNode(placeholder);

            var request = new CreateNodeRequest
            {
                title = placeholder.title,
                content = placeholder.content,
                position = new Vec3Data { x = position.x, y = position.y, z = position.z },
                space_id = CurrentSpaceId,
            };

            ApiClient.Instance.CreateNode(request, data =>
            {
                placeholder.id = data.id;
                nodesById[data.id] = view;
            }, err =>
            {
                Debug.LogError(err);
                Destroy(view.gameObject);
            });
        }

        public void SaveNode(NodeView node)
        {
            var request = new UpdateNodeRequest
            {
                title = node.Data.title,
                content = node.Data.content,
                position = new Vec3Data { x = node.Data.position_x, y = node.Data.position_y, z = node.Data.position_z },
            };

            ApiClient.Instance.UpdateNode(node.Data.id, request, _ => { }, Debug.LogError);
        }

        public void DeleteNode(NodeView node)
        {
            // Same optimistic pattern as create: remove it immediately, let
            // the API call finish in the background rather than blocking the
            // visual removal on a round trip.
            nodesById.Remove(node.Data.id);
            Destroy(node.gameObject);
            ApiClient.Instance.DeleteNode(node.Data.id, () => { }, Debug.LogError);
        }
    }
}
