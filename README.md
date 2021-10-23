# Squinject
In the 1.3.x revision of Endlesss, they have changed how soundpacks are managed. Previously packs were delivered as renamed .zip files full of instrument data (a JSON and 1 or more .ogg samples). These were decompressed on startup into a transient directory full of instruments in a big pile.

The new system uses another Couchbase Lite database, synced from the Endlesss server, to deliver soundpack metadata dynamically. This also means it can be updated live without having to release a new version of the product each time. Neat! But also this breaks Squonker's ability to inject itself into Studio/iOS as the previous system has been completely removed.

Squinject is a workaround that takes the compiled outputs from Squonker, reformats them and injects them into the local Couchbase sqlite3 database & sample cache so that they appear correctly alongside all the regular packs.

It's rough and ready currently, but does work fine!

