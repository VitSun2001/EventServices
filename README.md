# EventGenerator и EventProcessor
- у EventGenerator можно настроить минимальную `EventGeneratorOptions.MinDelayBetweenEventsMillis` и максимальную `EventGeneratorOptions.MaxDelayBetweenEventsMillis` задержку перед генерацией новго события.
- Uri для отправки запросов указывается в `EventServiceOptions.EventProcessorEndpoint`
- У EventProcessor есть возможность запуска как, с Sqlite, так и с Postgres, используемая БД определяется окружением appsettings.json, создание БД и применение миграции произойдет автоматически.
- C помощью параметра `EventProcessorOptions.IncidentGracePeriodMillis` можно изменить максимально допустимое окно для формирования инцидента второго типа.
- У EventProcessor есть два варианта обработки входящих событий, один основан на minimal api, другой на BackgroundService и HttpListener.
- - Для переключения режима необходимо поставить в appsettings поля `EventProcessorOptions.UseAlternativeIncidentPipeline` на true и `EventProcessorOptions.AlternativeIncidentHttpListenerUri` на который будут приходить запросы, также будет необходимо сменить соответстующий порт в appsettings у EventGenerator.