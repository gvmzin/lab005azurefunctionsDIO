using Azure.Messaging.ServiceBus;
using BarcodeStandard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace fnGeradorBoletos;

public class GeradorCodigoBarras
{
    private readonly ILogger<GeradorCodigoBarras> _logger;
    private readonly string _serviceBusConnectionString;
    private readonly string _queueName = "gerador-codigo-barras-queue";

    public GeradorCodigoBarras(ILogger<GeradorCodigoBarras> logger)
    {
        _logger = logger;
        _serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString");
    }

    [Function("barcode-generate")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        try
        {

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            string valor = data?.valor;
            string dataVencimento = data?.dataVencimento;

            string barcodeData;

            //Validação dos DADOS
            if (string.IsNullOrEmpty(valor) || string.IsNullOrEmpty(dataVencimento))
            {
                return new BadRequestObjectResult("Os campos valor e datavencimento são obrigatórios.");
            }

            // Validar formato da data de vencimento YYYY-MM-DD
            if(!DateTime.TryParseExact(dataVencimento, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime dateObj))
            {
                return new BadRequestObjectResult("Data de vencimento invalida.");
            }

            string dateStr = dateObj.ToString("yyyyMMdd");

            // Conversão valor para centavos e formatação até 8 digitos
            if(!decimal.TryParse(valor, out decimal valorDecimal))
            {
                return new BadRequestObjectResult("Valor invalido");
            }
            int valorCentavos = (int)(valorDecimal * 10);
            string valorStr = valorCentavos.ToString("D8");

            string bankCode = "008";
            string baseCode = string.Concat(bankCode, dateStr, valorStr);

            // preenchimento do barcode 44 digitos
            barcodeData = baseCode.Length < 44 ? baseCode.PadRight(44, '0') : baseCode.Substring(0, 44);
            _logger.LogInformation($"Barcode gerado: {barcodeData}");

            Barcode barcode = new Barcode();
            var skImage = barcode.Encode(BarcodeStandard.Type.Code128, barcodeData);

            using (var encodeData = skImage.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
            {
                var imageBytes = encodeData.ToArray();

                string base64String = Convert.ToBase64String(imageBytes);


                var resultObject = new
                {
                    barcode = barcodeData,
                    valorOriginal = valorDecimal,
                    DataVencimento = DateTime.UtcNow.AddDays(5),
                    ImagemBase64 = base64String
                };
                await SendFileFallback(resultObject, _serviceBusConnectionString, _queueName);
                return new OkObjectResult(resultObject);

            }


        }
        catch (Exception ex)
        {
            // Corrigido: usar constante inteira de StatusCodes em vez de enum incompatível.
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    private async Task SendFileFallback(object resultObject, string serviceBusConnectionString, string queueName)
    {
        await using var cliente = new ServiceBusClient(serviceBusConnectionString);
        ServiceBusSender sender = cliente.CreateSender(queueName);
        string messageBody = System.Text.Json.JsonSerializer.Serialize(resultObject);
        ServiceBusMessage message = new ServiceBusMessage(messageBody);
        await sender.SendMessageAsync(message);
        _logger.LogInformation("Mensagem enviada pra fila: {QueueName}", queueName);
    }
}