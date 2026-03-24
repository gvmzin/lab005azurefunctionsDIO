# 📘 Documentação Completa: Projeto Lab005 - Gerador e Validador de Boletos

Este documento descreve o passo a passo completo da implementação do projeto Lab005, que integra o Azure Service Bus, Azure Functions (.NET/C#) e um Front-end para geração e validação de códigos de barras (boletos).

---

## 🏗️ 1. Configuração no Azure: Service Bus

O **Azure Service Bus** é utilizado como um serviço de mensagens para garantir a persistência dos boletos gerados.

### Passos para criação:
1.  **Criar Namespace**:
    *   No Portal do Azure, crie um novo recurso de **Service Bus**.
    *   Escolha um nome único para o Namespace (ex: `ns-dio-boletos`).
    *   Selecione o plano (Basic é suficiente para este laboratório).
2.  **Criar a Fila (Queue)**:
    *   Dentro do Namespace, vá em **Queues**.
    *   Crie uma nova fila chamada `gerador-codigo-barras-queue`.
3.  **Obter a String de Conexão**:
    *   Vá em **Shared access policies**.
    *   Clique em `RootManageSharedAccessKey` (ou crie uma nova).
    *   Copie a **Primary Connection String**. Ela será usada na configuração local da Function.

---

## ⚡ 2. Desenvolvimento das Azure Functions

O projeto utiliza o modelo de **Azure Functions Isolated Worker** em C#.

### A. Função de Geração (`barcode-generate`)
*   **Trigger**: HTTP POST.
*   **Objetivo**: Receber valor e data, gerar um código de barras de 44 dígitos, enviar para a fila do Service Bus e retornar a imagem (Base64) para o cliente.
*   **Lógica de Código**:
    *   Valida os campos `valor` e `dataVencimento`.
    *   Gera uma string numérica baseada no banco (008), data e valor.
    *   Usa a biblioteca `BarcodeStandard` para gerar a imagem.
    *   **Integração Service Bus**: Chama o método `SendFileFallback` para enviar o JSON do boleto para a fila.

### B. Função de Validação (`barcode-validate`)
*   **Trigger**: HTTP POST.
*   **Objetivo**: Validar se um código de barras enviado é legítimo.
*   **Lógica de Código**:
    *   Verifica se o código possui exatamente 44 dígitos.
    *   Tenta extrair a data de vencimento embutida no código.
    *   Retorna um JSON indicando `valido: true` ou `false`.

### C. Configuração Local (`local.settings.json`)
Certifique-se de configurar a variável de ambiente para a fila:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ServiceBusConnectionString": "SUA_CONNECTION_STRING_AQUI"
  }
}
```

---

## 💻 3. Desenvolvimento do Front-end

O front-end é uma interface simples em HTML/CSS/JS para interagir com as APIs locais.

### Funcionalidades:
1.  **Formulário**: Captura a data de vencimento e o valor.
2.  **Chamada API Geradora**: Faz um `fetch` para `http://localhost:7023/api/barcode-generate`.
3.  **Exibição**: Mostra o código de barras e a imagem gerada dinamicamente via Base64.
4.  **Validação**: Um botão secundário que envia o código para `http://localhost:7210/api/barcode-validate`.
    *   **Feedback Visual**: O fundo do código muda para verde (válido) ou vermelho (inválido).

---

## 🚀 4. Validação Local e Testes

Para testar o fluxo completo:

1.  **Executar as Functions**: 
    *   No VS Community ou via CLI (`func start`), inicie ambos os projetos de Function.
    *   Verifique se as URLs `localhost:7023` e `localhost:7210` estão ativas.
2.  **Postman (Opcional)**:
    *   Envie um POST para `/api/barcode-generate` com `{"valor": "100.00", "dataVencimento": "2024-12-31"}`.
3.  **Navegador**:
    *   Abra o arquivo `front/index.html`.
    *   Gere um boleto e valide-o imediatamente.
4.  **Service Bus Explorer**:
    *   No Portal do Azure, use o "Service Bus Explorer" para verificar se as mensagens estão chegando na fila `gerador-codigo-barras-queue`.

---

## 📂 Estrutura do Projeto
```text
lab005/
├── DioFuncHttp/          # Exemplo de Function HTTP básica
├── fnGeradorBoletos/     # Solução Principal
│   ├── fnGeradorBoletos/ # Projeto da Function Geradora
│   └── fnValidaBoleto/   # Projeto da Function Validadora
├── front/                # Interface Web (HTML/CSS/JS)
└── *.png                 # Capturas de tela do setup no Azure
```

---

> [!TIP]
> Lembre-se de habilitar o **CORS** nas Azure Functions ou no navegador para permitir que o front-end acesse as APIs em portas diferentes durante o desenvolvimento local.
