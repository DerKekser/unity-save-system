namespace Game.Scripts.SaveSystem.Data
{
    public interface IData
    {
        void DataSerialize(SaveBuffer saveBuffer);
        void DataDeserialize(SaveBuffer saveBuffer);
    }
}