using System;

namespace Nodia.Data
{
    [Serializable]
    public class Vec3Data
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class NodeData
    {
        public string id;
        public string title;
        public string content;
        public float position_x;
        public float position_y;
        public float position_z;
    }

    [Serializable]
    public class ConnectionData
    {
        public string id;
        public string from_node;
        public string to_node;
    }

    // A "space" here is a named workspace a user can switch between (e.g. one
    // per class) - distinct from SpaceSnapshot, which is the nodes+connections
    // *inside* one such space.
    [Serializable]
    public class SpaceData
    {
        public string id;
        public string name;
    }

    [Serializable]
    public class SpaceSnapshot
    {
        public NodeData[] nodes;
        public ConnectionData[] connections;
    }

    [Serializable]
    public class CreateSpaceRequest
    {
        public string name;
    }

    [Serializable]
    public class SpacesListResponse
    {
        public SpaceData[] spaces;
    }

    [Serializable]
    public class CreateNodeRequest
    {
        public string title;
        public string content;
        public Vec3Data position;
        public string space_id;
    }

    [Serializable]
    public class UpdateNodeRequest
    {
        public string title;
        public string content;
        public Vec3Data position;
    }

    [Serializable]
    public class CreateConnectionRequest
    {
        public string from_node;
        public string to_node;
        public string space_id;
    }
}
