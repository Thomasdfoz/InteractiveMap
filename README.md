# Unity Interactive Tile-Based Map System

Este projeto fornece um sistema de mapa interativo em Unity, baseado em tiles personalizados hospedados em seu próprio servidor. Ele suporta pan, zoom centrado no mouse e colocação de marcadores (pins) conforme coordenadas geográficas.

## 📦 Instalação

Siga os passos abaixo para adicionar o pacote diretamente pelo Git URL no Unity:

1. Abra seu projeto no Unity.  
2. No menu superior, vá em **Window > Package Manager**.  
3. Clique no botão **+** no canto superior esquerdo e selecione **Add package from Git URL...**.  
4. Cole a URL do repositório Git:
    ```text
  [ https://github.com/SEU_USUARIO/NOME_DO_REPOSITORIO.git](https://github.com/Thomasdfoz/InteractiveMap.git)
   ```
5. Clique em **Add** e aguarde o pacote ser importado.

> **Obs.:** Substitua `SEU_USUARIO/NOME_DO_REPOSITORIO` pela URL do seu repositório.

## ⚙️ Requisitos

- Unity **2020.3** ou superior  
- .NET Standard 2.0  
- Servidor de tiles acessível via HTTP com URL no formato:
  ```text
  http://<seu-servidor>/{z}/{x}/{y}.png
  ```

## 🔧 Configuração

1. Na pasta `Assets`, crie um **ScriptableObject** de `MapConfig` (**Assets > Create > Maps Objects > MapConfig**).  
2. Preencha os campos:
   - **Name:** Identificador do mapa (ex: `GlobalMap`).  
   - **URL:** URL base do servidor de tiles (ex: `http://localhost:8080/tiles`).  
   - **Zoom:** Nível de zoom inicial.  
   - **Range:** Quantidade de tiles em volta do centro.  
   - **CenterLat / CenterLon:** Coordenadas iniciais do centro.  
   - **TilePixelSize:** Tamanho de cada tile (padrão 256).  
3. Arraste o `MapConfig` criado para o componente **GlobalManager** na cena.  
4. Ajuste referências no **GlobalManager**:
   - **Canvas**: Canvas que conterá o mapa.  
   - **Tile Prefab**: Prefab para cada tile (deve conter um componente `Tile` com `Image`).  
   - **TileDownloader**: Componente responsável pelo download das imagens.  
   - **InputController**: Controla pan/zoom/pins.  
   - **Default Sprite**: Sprite padrão para tiles sem textura.  

## 🚀 Como Usar

1. Execute a cena que contenha o **GlobalManager**.  
2. Utilize a roda do mouse para dar **zoom** (centrado na posição do cursor).  
3. Clique e arraste para **pan**.  
4. Para adicionar um pin, ative o modo de pin (`InputController.m_addPin = true`) e clique no mapa; um marcador será adicionado na coordenada clicada.  

## 📂 Estrutura do Projeto

```
Assets/
├── Scripts/
│   ├── MapUtils.cs         // Conversões entre lat/lon e pixels
│   ├── TileDownloader.cs   // Download assíncrono de tiles
│   ├── Tile.cs             // Exibe o sprite do tile
│   ├── GlobalManager.cs    // Orquestra mapa global e mapas náuticos
│   ├── MapManager.cs       // Gerencia container e TileManager
│   ├── TileManager.cs      // Cria, posiciona e recicla tiles
│   ├── PinData.cs          // Armazena lat/lon do pin
│   ├── PinManager.cs       // Cria e atualiza pins
│   ├── InputController.cs  // Lida com pan, zoom e adição de pins
│   ├── MapConfig.cs        // ScriptableObject com configurações do mapa
│   └── MapSettings.cs      // Struct serializável para configurações internas
└── Prefabs/
    ├── Tile.prefab         // Prefab do tile (Image)
    └── Pin.prefab          // Prefab do marcador (UI)
```

## ✨ Funcionalidades

- **Pan** suave com clique e arraste.  
- **Zoom** centrado na posição do mouse.  
- **Cache** de objetos de tile via `ObjectPool` para performance.  
- Suporte a **vários mapas** (global e náuticos) na mesma cena.  
- **Marcadores (Pins)** posicionados via coordenadas geográficas.  

## 🤝 Contribuição

1. Fork este repositório.  
2. Crie uma branch com sua feature:  
   ```bash
   git checkout -b feature/nova-feature
   ```  
3. Faça commit das suas mudanças:  
   ```bash
   git commit -m "Adiciona nova feature"
   ```  
4. Envie para o repositório remoto:  
   ```bash
   git push origin feature/nova-feature
   ```  
5. Abra um Pull Request.  

## 📄 Licença

Este projeto está licenciado sob a licença MIT. Consulte o arquivo [LICENSE](LICENSE) para mais detalhes.  
