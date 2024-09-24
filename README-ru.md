<h1 align="center">
  <img src="Assets/logo.png" alt="logo.png" width="256"/>
  <br/>
  Adaptive DMI Tool
</h1>

README Доступные языки:

[English](https://github.com/PhantornRU/AdaptiveSpritesDMItool/blob/main/README.md)

[Russian](https://github.com/PhantornRU/AdaptiveSpritesDMItool/blob/main/README-ru.md)

# Инструмент адаптирования .dmi файлов
Данный инструмент предназначен для редактирования .dmi файлов с потенциалом для адаптации их на любые формы, размеры, оффсеты и другие параметры накладываемые на все выбранные файлы через редакцию конфига, хранящую табличные данные пикселей и их смещения.

## Для реализации использовались:
* Интерфейс [WPF](https://github.com/dotnet/wpf) фреймворк.
* Фреймворк обработки DMI файлов [DMI Sharp](https://github.com/bobbah/DMISharp)


## Пользование

### Страницы
В программе 3 страницы:
* Home - [WIP] Страница для работы с рабочим пространством, сохранение последних настроек пользователя, загрузка предустановок. 
* Edit - Страница редактирования конфигов через изменения пикселей на превью полотках.
* Data - Страница обработки файлов под выбранные конфиги.

### Home
[WIP]

### Edit

1 - Выбор страницы

2 - Тулбар, кнопки для взаимодействия и редактирования ПРЕВЬЮ. Редактирование, удаление, смена режима (параллель), сетка, оверлей. Тулбар инструменты могут не полностью отобразиться, вы можете отобразить полные инструменты расширив окно или нажав на кнопку "галочку" выпадения справа.

3 - Превью изображения отображающей все редактированные спрайты под конфиги, оверлеи (редактируемые изображения), превью (левое нередактируемое изображение) и лендмарки (правое нередактируемое изображение).

4 - Статус бар информации мыши находящейся поверх окон превью.

5 - Окно загрузки ДМИ файлов и выбор из них ДМИ Стейтов для отображения поверх. Можно настроить поверх какого превью будет отображаться стейт.

6 - Окно сохранения и создания новых конфигов. 

Здесь можно: 

- Создать новый конфиг (НЕ ЗАБУДЬТЕ СОХРАНИТЬ ЕГО)

- Загрузить уже существующий конфиг

- Сохранить текущий конфиг

- Сохранить текущий конфиг как новый файл.

При выборе конфига - он накладывается поверх превью.

  <img src="Assets/1 Edit Page.png" alt="logo.png" width="512"/>
  
### Data

1 - Отображение всех файлов загруженных с директории "Импорта" для обработки в папку Экспорта.

- Кнопка "Override" переключит режим перезаписи похожих файлов в директории Экспорта.

2 - Отображение стейтов выбранного .dmi файла

3 - Панель выбора конфига для обработки всех файлов. Можно выделить сразу несколько конфигов. Название конфига будет использовано как название новой папки в директории Экспорта.

4 - Установка путей директорий Импорта и Экспорта. По дефолту файлы будут обработаны в папке рабочего билда.

5 - Полоска загрузки отображающая сколько файлов уже было обработано.

6 - Кнопка обработки всех файлов под выбранные конфиги.

После нажатия - дождитесь его завершения.

Все обработанные файлы будут экспортированы в Директория/"Название Конфига"/

  <img src="Assets/2 Data Page.png" alt="logo.png" width="512"/>

## Delelopment
Программа разделена на контроллеры, ресурсы, модели и вспомогательные классы для более удобного доступа к коду. Внутри кода имеются разделения на региона для еще более удобной навигации и разделения. 

### For .NET 7
These tool require Visual Studio 2022(v17.7), Visual Studio 2022 for Mac (v17.6) to build, test, and deploy, and also require the .NET 7 SDK.

[Get a free copy of Visual Studio 2022 Community Edition](https://www.visualstudio.com/wpf-vs)

[.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)

### View Models
Помимо Views в которых находятся WPF страницы, ими используются:
* Dashboard View Model - [WIP]
* States Editor View Model - модель реализации изменения выбора конфигов, превью стейтов, работы с файлами и их загрузкой, сохранением конфига и хранения State Item's.
* Data View Model - модель реализации изменения выбранного конфига, отображения Tree View со всеми выбранными файлами которые будут отображаться и обрабабатываться вдальнейшем. 
* Settings View Model - модель настроек текущей темы.
* Main Window View Model - модель навигации по страницам.

### Models
* Config Item - модель конфига хранящая путь к таблице для быстрого доступа.
* State Item - модель стейта с .DMI файла хранящая превью, путь, название файла и название стейта.
* Data Image State - модель работы с превью изображениями накладывающие стейты "превью, лендмарки и оверлея" друг на друга для дальнейшей визуализации.
* State Edit Type - модель перечисления различных типов:
** StateEditType - Режим редактирования главного окна предварительного просмотра
** StateQuantityType - Тип редактирования стейтов.
** StateImageType - Тип элемента превью.
** StateImageSideType - Сторона элемента превью.
** SelectMode - Текущий режим инструмента перемещения пикселей.
** StatusBarType - Тип элемента статус бара.
** StatePreviewType - Выбранный тип превью для наложения стейтов.

### Controllers 
* Environment Controller - Контроллер рабочего пространства и инициализации окружения.
* Draw Controller - Контроллер обработки изображения, хранящий все функции для рисования на полотне и редактирования пикселей.
* Editor Controller - Контроллер текущих режимов редактирования полотен и логики кнопок.
* Mouse Controller - Контроллер обработки нажатых кнопок мыши и нахождения позиции курсора на полотне.
* Buttons Controller - Контроллер обработки нажатых клавиш, хоткеев.
* States Controller - Контроллер хранящий информацию о текущих состояниях, режимах, конфигах и статусах элементов используемых всей программой.
* Status Bar Controller - Контроллер информации отображаемой на статусной панели.

### Processors
DMI State Processor - Обработчик файлов из данных стейтов под выделенные конфиги.

### Helpers 
Image Encoder - Обработчик DMI State в Writeable Bitmap для отображения и редактирования на странице "Edit".
Files Searcher - Поисковик корректной директории.



## Связь
Вопросы или хотите в помочь реализации? Можете связаться со мной по дискорду: **PHANTOMRU** (не путайте ник на Гитхабе, там "m == rn" xdd)



# ENG:

## Tool for adapting .dmi files
This tool is designed to edit .dmi files with the potential to adapt them to any shape, size, offsets and other parameters imposed on all selected files through the config editor, storing tabular pixel data and their offsets.

### The following were used for implementation:
* Interface [WPF](https://github.com/dotnet/wpf) framework.
* Framework for processing DMI files [DMI Sharp](https://github.com/bobbah/DMISharp)

#### For .NET 7
These tool require Visual Studio 2022(v17.7), Visual Studio 2022 for Mac (v17.6) to build, test, and deploy, and also require the .NET 7 SDK.

[Get a free copy of Visual Studio 2022 Community Edition](https://www.visualstudio.com/wpf-vs)

[.NET 7 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)

## Usage

### Pages
The program has 3 pages:
* Home - [WIP] Page for working with the workspace, saving the latest user settings, loading presets.

* Edit - Page for editing configs by changing pixels on preview canvases.
* Data - Page for processing files for selected configs.

### Home
[WIP]

### Edit

### Data

## Delelopment
The program is divided into controllers, resources, models and auxiliary classes for more convenient access to the code. Inside the code there are divisions into regions for even more convenient navigation and separation.

### View Models
In addition to Views that contain WPF pages, they use:
* Dashboard View Model - [WIP]
* States Editor View Model - a model for implementing changes to config selection, preview states, working with files and loading them, saving configs and storing State Item's.
* Data View Model - a model for implementing changes to the selected config, displaying Tree View with all selected files that will be displayed and processed in the future.
* Settings View Model - a model for settings of the current theme.
* Main Window View Model - a model for navigating through pages.

### Models
* Config Item - a model for a config that stores the path to a table for quick access.
* State Item - a model for a state with a .DMI file that stores a preview, path, file name and state name.
* Data Image State - a model for working with preview images that superimpose "preview, landmark and overlay" states on top of each other for further visualization.
* State Edit Type - model of enumeration of various types:
** StateEditType - Editing mode of the main preview window
** StateQuantityType - Type of state editing.
** StateImageType - Type of preview element.
** StateImageSideType - Side of the preview element.
** SelectMode - Current mode of the pixel moving tool.
** StatusBarType - Type of the status bar element.
** StatePreviewType - Selected preview type for overlaying states.

### Controllers
* Environment Controller - Workspace and environment initialization controller.
* Draw Controller - Image processing controller that stores all functions for drawing on the canvas and editing pixels.
* Editor Controller - Controller of current canvas editing modes and button logic.
* Mouse Controller - Controller for processing pressed mouse buttons and finding the cursor position on the canvas.
* States Controller - Controller that stores information about current states, modes, configs and statuses of elements used by the entire program.
* Status Bar Controller - Controller of information displayed on the status bar.

### Processors
DMI State Processor - File processor from these states for selected configs.

### Helpers
Image Encoder - DMI State processor in Writeable Bitmap for display and editing on the "Edit" page.
Files Searcher - Search engine for the correct directory.

## Contact
Questions or want to help with the implementation? You can contact me on discord: **PHANTOMRU** (don't confuse the nickname on GitHub, there "m == rn" xdd)






# License
Unless otherwise mentioned, the samples are released under the [MIT license](https://github.com/PhantornRU/AdaptiveSpritesDMItool/blob/main/LICENSE)
