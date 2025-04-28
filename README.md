# Unity Interactive Tile-Based Map System

Este projeto fornece um sistema de mapa interativo em Unity, baseado em tiles personalizados hospedados em seu próprio servidor. Ele suporta pan, zoom centrado no mouse e colocação de marcadores (pins) conforme coordenadas geográficas.

## 📦 Instalação

Siga os passos abaixo para adicionar o pacote diretamente pelo Git URL no Unity:

1. Abra seu projeto no Unity.
2. No menu superior, vá em **Window > Package Manager**.
3. Clique no botão **+** no canto superior esquerdo e selecione **Add package from Git URL...**.
4. Cole a URL do repositório Git:
   ```text
   https://github.com/Thomasdfoz/InteractiveMap.git
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

1. **Canvas para o mapa**

   - Crie um _Canvas_ com _UI Scale Mode_ em **Constant Pixel Size** (tamanho de referência 1–100).
   - Ou use o prefab de exemplo **Canvas Map Sample**.

2. **RawImage para tiles**

   - Crie um _RawImage_ vazio (128×128) e adicione o script **Tile Renderer**.
   - Ou use o prefab **tile - RawImage** do sample.

3. **Default Texture**

   - Crie uma textura padrão branca (ou de sua preferência).
   - Ou use os prefabs **DefaultTextureBlack** ou **DefaultTextureWhite**.

4. **MapConfig (Global e Náutico)**

   - Clique com o botão direito em **Assets > Create > Maps Objects > MapConfig**.
   - Preencha apenas **Name** e **URL** (sem coordenadas), ex:
     ```
     Name: AmericasSouth
     URL: http://localhost:8080/styles/maptiler-basic
     ```
   - Os demais campos (Zoom, Range, CenterLat/CenterLon, TilePixelSize) serão preenchidos automaticamente.

5. **GameObject com Managers**

   - Crie um _Empty GameObject_ e adicione os scripts **GlobalManager**, **TileDownloader** e **MapController**.

6. **Configurar GlobalManager**

   - Arraste os prefabs e referências criados:
     - **Tile Prefab**: tamanho deve ser 128 (mesmo do RawImage).
     - **Default Sprite**: textura padrão criada.
     - **TileDownloader**: componente adicionado.
     - **Center Reference**: crie uma _UI Image_ no centro do Canvas, defina posição (0,0) e desative o componente _Image_ (deixe só o Transform).
   - Defina **Initial Lat/Lon** (qualquer um da América do Sul), ex:
     ```
     Lat: -23.099999904632568
     Lon: -48.200000762939453
     ```
   - Defina **Initial Zoom** (ideal 6; permitido 5–18).
   - No array **Maps**, adicione o MapConfig global e os MapConfigs náuticos.
   - Configure **LatOffset** e **LonOffset** para limites de bounds dos mapas náuticos (0 = somente aparece no centro).

7. **Configurar InputController**

   - Use seu script de controle personalizado ou o exemplo **InputController** dos samples, e aponte para o **MapController** como intermediário.

8. **Canvas UI Sample (opcional)**

   - Adicione o prefab **Canvas UI Sample** para ter exemplos de pins e info overlay.
   - Configure as referências em **PinLibrary**, **FlyTo**, e **Info**.

9. **Pronto!**
   - Agora basta clicar em **Play** e o mapa estará funcional.

## 🚀 Como Usar

1. Execute a cena que contenha todos os scripts descritos acima.
2. Utilize a roda do mouse para dar **zoom** (centrado na posição do cursor).
3. Clique e arraste para **pan**.
4. Para adicionar um pin, clique em algum do canto inferior e clique com o botao direito no mapa; um marcador será adicionado na coordenada clicada.

## 📂 Estrutura do Projeto

```
InteractiveMap/
└── Package/
    ├── Editor/
    ├── Runtime/
    │   ├── Core/
    │   ├── Data/
    │   ├── Pins/
    │   ├── Tiles/
    │   └── Utils/
    ├── Sample/
    │   ├── Maps/
    │   ├── Materials/
    │   └── Pins/
    │       ├── Prefabs/
    │       ├── Prefabs/
    │       └── Texture/
    └── Package Creator/
```

## ✨ Funcionalidades

- **Pan** suave com clique e arraste.
- **Zoom** centrado na posição do mouse.
- **Cache** de objetos de tile via `ObjectPool` para performance.
- Suporte a **vários mapas** (global e náuticos) na mesma cena.
- **Marcadores (Pins)** posicionados via coordenadas geográficas.

## 📄 Licença

Este projeto está licenciado sob a licença MIT. Consulte o arquivo [LICENSE](LICENSE) para mais detalhes.
