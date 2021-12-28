# Squinject
In the 1.3.x revision of Endlesss, they have changed how soundpacks are managed. Previously packs were delivered as renamed .zip files full of instrument data (a JSON and 1 or more .ogg samples). These were decompressed on startup into a transient directory full of instruments in a big pile.

The new system uses another Couchbase Lite database, synced from the Endlesss server, to deliver soundpack metadata dynamically. This also means it can be updated live without having to release a new version of the product each time. Neat! But also this breaks Squonker's ability to inject itself into Studio/iOS as the previous system has been completely removed.

Squinject is a workaround that takes the compiled outputs from Squonker, reformats them and injects them into the local Couchbase sqlite3 database & sample cache so that they appear correctly alongside all the regular packs.

It's much clunkier than the original soundpack hack, but it does work!

1. Build Squinject
2. Create a subdirectory in your Squonker root for it to live in, eg. `D:\Squonker\Squinject`
3. Stuff the build in there
4. Build your soundpacks using Squonker
5. open a terminal in your Squonker root ... 


### *Injecting into Endlesss Studio*

 run 
 
 `squinject/squinject.exe studio`

 Squinject will read your studio installation directory from the registry.

<br>

### *Injecting into iOS* 

`squonker ios-injection-pull`

(this fetches the database from the device, saved into `.\injection_db`)

`squinject/squinject ios`

(this runs the injection process against that downloaded directory)

`squonker ios-injection-push`

(this pushes the changes back to iOS. Note as of writing I haven't finished this bit yet)

