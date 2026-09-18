namespace BuildingBlocks.Common.Extensions
{
    public static class EventoHelper
    {
        public static string ChaveDeParticionamento(Guid idConta, DateOnly data) => $"{idConta}|{data:yyyyMMdd}";
    }
}
