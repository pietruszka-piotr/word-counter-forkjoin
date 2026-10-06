# Równoległy licznik słów

Aplikacja konsolowa C#/.NET 8 zliczająca słowa w plikach `.txt`, także w podkatalogach. `Parallel.ForEach` rozdziela pliki pomiędzy wątki; każdy plik jest czytany strumieniowo, bez ładowania całej zawartości do pamięci.

## Uruchomienie

Wymagany jest SDK .NET 8 lub nowszy z obsługą aplikacji .NET 8. Polecenia wykonuj w katalogu repozytorium:

```bash
dotnet build wspolbiezne_3.sln
dotnet run --project wspolbiezne_3.csproj --no-launch-profile -- samples/input samples/output
```

Przykładowy plik zawiera 8 słów. W katalogu `samples/output` pojawią się `log.txt` ze statystykami wątków i `wyniki.csv` z liczbą słów dla każdego pliku. Dla własnych danych podaj folder wejściowy i folder wyników; ścieżki ze spacjami ujmij w cudzysłowy. Bez drugiego argumentu wynik trafia do katalogu `wyniki` w bieżącym katalogu.

## Zasady liczenia

- Słowo to ciąg znaków rozdzielony białymi znakami. `hello,world` jest jednym słowem; interpunkcja nie jest osobnym separatorem.
- Stan słowa jest zachowany między blokami odczytu, więc słowo przecięte granicą bufora zostaje policzone raz.
- Katalog wyników może być podkatalogiem wejścia; jego pliki są pomijane. Nie może być tym samym katalogiem co wejście.
- Równoległość jest ograniczona do `Environment.ProcessorCount`. Suma jest aktualizowana przez `Interlocked`, a wyniki trafiają do `ConcurrentBag`.

## Testy

```bash
dotnet test wspolbiezne_3.sln
```

Testy obejmują słowa i białe znaki na granicy bufora, pusty plik oraz pomijanie własnych raportów. Obecnie błąd odczytu jednego pliku przerywa całe przetwarzanie; aplikacja zakłada dostępne pliki tekstowe UTF-8 lub z rozpoznawalnym BOM.
