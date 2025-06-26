device-pda-slot-component-slot-name-cartridge = Картридж
default-program-name = Программа
notekeeper-program-name = Заметки
news-read-program-name = Новости станции
crew-manifest-program-name = Манифест экипажа
crew-manifest-cartridge-loading = Загрузка...
net-probe-program-name = Зонд сетей
net-probe-scan = Просканирован { $device }!
net-probe-label-name = Название
net-probe-label-address = Адрес
net-probe-label-frequency = Частота
net-probe-label-network = Сеть
log-probe-program-name = Зонд логов
log-probe-scan = Загружены логи устройства { $device }!
log-probe-label-time = Время
log-probe-label-accessor = Использовано:
log-probe-label-number = #
astro-nav-program-name = АстроНав
med-tek-program-name = МедТек
# Wanted list cartridge
wanted-list-program-name = Список разыскиваемых
wanted-list-label-no-records = Всё спокойно, ковбой.
wanted-list-search-placeholder = Поиск по имени и статусу
wanted-list-age-label = [color=darkgray]Возраст:[/color] [color=white]{ $age }[/color]
wanted-list-job-label = [color=darkgray]Должность:[/color] [color=white]{ $job }[/color]
wanted-list-species-label = [color=darkgray]Раса:[/color] [color=white]{ $species }[/color]
wanted-list-gender-label = [color=darkgray]Гендер:[/color] [color=white]{ $gender }[/color]
wanted-list-reason-label = [color=darkgray]Причина:[/color] [color=white]{ $reason }[/color]
wanted-list-unknown-reason-label = неизвестная причина
wanted-list-initiator-label = [color=darkgray]Инициатор:[/color] [color=white]{ $initiator }[/color]
wanted-list-unknown-initiator-label = неизвестный инициатор
wanted-list-status-label = [color=darkgray]статус:[/color] { $status ->
        [suspected] [color=yellow]подозревается[/color]
        [wanted] [color=red]разыскивается[/color]
        [detained] [color=#b18644]под арестом[/color]
        [paroled] [color=green]освобождён по УДО[/color]
        [discharged] [color=green]освобождён[/color]
       *[other] нет
    }
wanted-list-history-table-time-col = Время
wanted-list-history-table-reason-col = Преступление
wanted-list-history-table-initiator-col = Инициатор

nano-task-program-name = Нанозадача

log-probe-print-button = Печатные журналы

log-probe-printout-device = Отсканированное устройство: {$ name}

log-probe-printout-header = Последние журналы:

log-probe-printout-entry = #{$ number} / {$ Time} / {$ Accessor}

nano-task-ui-heading-high-priority-tasks = {$ сумма ->
[Zero] Нет высоких приоритетных задач
[один] 1 задача высокого приоритета
*[Другое] {$ сумма} Высокие приоритетные задачи
}

nano-task-ui-heading-medium-priority-tasks = {$ сумма ->
[Zero] Нет средних приоритетных задач
[один] 1 Средний приоритетный задача
*[Другое] {$ сумма} Средние приоритетные задачи
}

nano-task-ui-heading-low-priority-tasks = {$ сумма ->
[Zero] Нет низких приоритетных задач
[один] 1 задача с низким приоритетом
*[Другое] {$ сумма} Низкие приоритетные задачи
}

nano-task-ui-done = Сделанный

nano-task-ui-revert-done = Отменить

nano-task-ui-priority-low = Низкий

nano-task-ui-priority-medium = Середина

nano-task-ui-priority-high = Высокий

nano-task-ui-cancel = Отмена

nano-task-ui-print = Печать

nano-task-ui-delete = Удалить

nano-task-ui-save = Сохранять

nano-task-ui-new-task = Новая задача

nano-task-ui-description-label = Описание:

nano-task-ui-description-placeholder = Получите что -нибудь важное

nano-task-ui-requester-label = Запрашивающая:

nano-task-ui-requester-placeholder = Джон Нанотразен

nano-task-ui-item-title = Редактировать задачу

nano-task-printed-description = Описание: {$ Описание}

nano-task-printed-requester = Запись: {$ requester}

nano-task-printed-high-priority = Приоритет: высокий

nano-task-printed-medium-priority = Приоритет: средний

nano-task-printed-low-priority = Приоритет: низкий
