namespace SCMPG
{
    public class ServerDescription
    {
        /// <summary>
        /// 地址
        /// </summary>
        public System.Net.IPEndPoint Address;
        /// <summary>
        /// 是否本地局域网
        /// </summary>
        public bool IsLocal;
        /// <summary>
        /// 发现时间
        /// </summary>
        public double DiscoveryTime;
        /// <summary>
        /// ping
        /// </summary>
        public float Ping;
        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority;

        public string Name;

        public Engine.DynamicArray<Game.WorldInfo> GameDescriptions = new Engine.DynamicArray<Game.WorldInfo>();
    }
}
