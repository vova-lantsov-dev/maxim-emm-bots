namespace MaximEmmBots.Models.Charts
{
    internal readonly struct PieChartItem
    {
        internal PieChartItem(in int weight, string text)
        {
            Weight = weight;
            Text = text;
        }
        
        public readonly int Weight;

        public readonly string Text;
    }
}