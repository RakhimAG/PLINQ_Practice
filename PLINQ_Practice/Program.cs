using System.Diagnostics;
using PLINQ_Practice;

static long HeavyCalculation(int number)
{
    long result = 0;

    for (int i = 1; i <= 10_000; i++)
    {
        result += (number * i) % 1234567;
    }


    return result;
}


var numbers = Enumerable.Range(1, 5_000_000);
long nonParallelElapsedMs = 1;
long parallelElapsedMs = 1;

{
    var nonParallelExecutionTime = Utils.MeasureExecution<IEnumerable<long>>
    (
        () =>
        {
            return numbers
            .Select(HeavyCalculation)
            .ToList();
        },
        out var result,
        out nonParallelElapsedMs
    );


    Console.WriteLine($"LINQ : \nTime : {nonParallelExecutionTime}");
}


{
    var parallelExecutionTime = Utils.MeasureExecution<IEnumerable<long>>
    (
        () => numbers
        .AsParallel()
        .Select(HeavyCalculation)
        .ToList(),

        out var result,
        
        out parallelElapsedMs
    );

    Console.WriteLine($"PLINQ : \nTime : {parallelExecutionTime}");
}

// ======= PERFOMANCE =======
Console.WriteLine($"SPEEDUP : {nonParallelElapsedMs / parallelElapsedMs}x");

{
    var executionTime1 = Utils.MeasureExecution<IEnumerable<long>>
    (
        () => numbers
        .AsParallel()
        .WithDegreeOfParallelism(2)
        .Select(HeavyCalculation)
        .ToList(),

        out var result1
    );

    Console.WriteLine($"WithDegreeOfParallelism(2) : {executionTime1}");


    var executionTime2 = Utils.MeasureExecution
    (
        () => numbers
        .AsParallel()
        .WithDegreeOfParallelism(4)
        .Select(HeavyCalculation)
        .ToList(),

        out var result2
    );

    Console.WriteLine($"WithDegreeOfParallelism(4) : {executionTime2}");


    var executionTime3 = Utils.MeasureExecution(
        () => numbers
        .AsParallel()
        .WithDegreeOfParallelism(8)
        .Select(HeavyCalculation)
        .ToList(),

        out var result3
    );

    Console.WriteLine($"WithDegreeOfParallelism(8) : {executionTime3}");
    
    var executionTime4 = Utils.MeasureExecution(
        () => numbers
        .AsParallel()
        .WithDegreeOfParallelism(16)
        .Select(HeavyCalculation)
        .ToList(),

        out var result4    
    );

    Console.WriteLine($"WithDegreeOfParallelism(16) : {executionTime4}");
}

var numbers2 = Enumerable.Range(1, 100);

{
    var resultNotOrdered = numbers2
                .AsParallel()
                .Select(x => x)
                .ToList();

    //foreach ( var number in resultNotOrdered)
    //    Console.WriteLine(number);

    var resultOrdered = numbers2
                        .AsParallel()
                        .AsOrdered()
                        .Select(x => x)
                        .ToList();

    //foreach( var number in resultOrdered)
    //    Console.WriteLine(number);
}


// Почему PLINQ может менять порядок?
/*

В данном случае используя параллельность порядок не поменялся
(Наверное их-за простоты операции, происходит оптимизации).
Но есть такой случай, где если попытаться использовать
параллельность над такой же задачой (С более сложным Select),
то потоки будут соревноваться между собой, и вставлять значение как попало.
В итоге, может получится смешанная коллекция, либо же не полная 
(если несколько потоков будут изменять коллекцию).
 
*/

// Для чего нужен AsOrdered()?
/*

Как раз чтобы исправлять случай который описал сверху.
А именно случай когда наш селект не записывает в нашу коллекцию,
а просто выполняет операции над элементами. Она не спасет если у нас
второй случай, который я описал (например Select( x => collection.Add(x*2)).
Второй случай исправляется использованием правильной дата структуры Concurrent типа.
Как именно AsOrdered() работает? Она сохраняет порядок каждого элемента перед 
операцием над элементом. Это главное отличие от OrderBy(), ведь он уже сортирует
конечный результат.
 
*/

//Есть ли цена у сохранения порядка?
/*

Да, происходит маппинг, разделение коллекции на саб-коллекции (мини коллекции),
для того чтобы дать гарантию сохранения позиции элемента в коллекции.

Когда его использовать?
    Полностью зависит от ситуации. Если порядок значение нужно возвращать неизмененным 
(например фронтенду), ведь там надеются на сохранения порядка, то цена имеется.
Просто нужно ответить на вопрос, значат ли порядок каждого элемента в нашей программе?

То есть его стоит использовать тогда, когда порядок имеет значение,
в противном случае для оптимизации лучше не использовать.
 */

// BONUS

{
    Stopwatch stopwatch = new Stopwatch();

    // LINQ
    var LINQExecuteTime = Utils.MeasureExecution
    (
        () => numbers2
              .Select(x => 2 * x)
              .ToList(),

        out var resultLINQ
    );
    Console.WriteLine($"LINQ On small collection Range(1,100) WITH USING MY WRAPPER : {LINQExecuteTime} TIME");

    stopwatch.Start();
    var a = numbers2
              .Select(x => 2 * x)
              .ToList();
    stopwatch.Stop();
    Console.WriteLine($"LINQ WITHOUT USING WRAPPER : {stopwatch} Time");

    // PLINQ
    var PLINQExecuteTime = Utils.MeasureExecution
    (
        () => numbers2
              .AsParallel()
              .AsOrdered()
              .Select(x => 2 * x)
              .ToList(),

        out var resultPLINQ
    );

    Console.WriteLine($"PLINQ On small collection Range(1,100) WITH USING MY WRAPPER : {PLINQExecuteTime} TIME");

    stopwatch.Restart();
    var b = numbers2
              .AsParallel()
              .AsOrdered()
              .Select(x => 2 * x)
              .ToList();
    stopwatch.Stop();
    Console.WriteLine($"PLINQ WITHOUT USING WRAPPER : {stopwatch} Time");

    /*
    Насчет этого кейса, у меня результат такой :

PLINQ On small collection Range(1,100) WITH USING MY WRAPPER : 00:00:00.0001436 TIME
PLINQ WITHOUT USING WRAPPER : 00:00:00.0000855 Time
LINQ On small collection Range(1,100) WITH USING MY WRAPPER : 00:00:00.0005977 TIME
LINQ WITHOUT USING WRAPPER : 00:00:00.0001124 Time
     
     мой метод помощник значительно замедляет LINQ запрос, но все равно то же самое происходит и с параллелью.
    И в итоге плинг быстрее получается над простым запросом. Есть теория что это связано с оптимизациями,
    которые происходят во время компиляции. 
        Я затем поменял их местами ( то есть первым линг затем плинг), результат стал более странным:

LINQ On small collection Range(1,100) WITH USING MY WRAPPER : 00:00:00.0005385 TIME
LINQ WITHOUT USING WRAPPER : 00:00:00.0000219 Time
PLINQ On small collection Range(1,100) WITH USING MY WRAPPER : 00:00:00.0001768 TIME
PLINQ WITHOUT USING WRAPPER : 00:00:00.0000858 Time

    Вот тут уже линг победил, НО без использования моего метода... я без понятие как это возможно.
    Я не проверял что будет если их отделить скопом. То есть в отдельных частях памяти чтобы находились
    и не видели друг-друга. У меня есть подозрение что именно так результат будет правильным (linq > plinq in time)
     */
}

// когда PLINQ действительно может ускорить программу;
/*

Если вычисления сложные, элементов в коллекции куча то стоит использовать PLINQ.

*/

//когда PLINQ использовать бессмысленно;
/* Если наш запрос является простым, как и сама коллекция (мало элементов)*/

//почему большая коллекция сама по себе не гарантирует ускорение;
// Зависит от тяжести вычисления, если использовать очень простой запрос над большой коллекцией,
// и используя PLINQ мы можем потерять время.

//почему CPU-bound операции являются хорошим кандидатом;
// Так как это является вычислетильным операцием, которая может выполняться несколькими ядрами параллельно

//как AsOrdered() влияет на выполнение;
// сказано что без данного метода, конечный результат не гаранитрован быть с тем же порядком до запроса.
// этот метод сохраняет позицию каждого элемента, и дает гарантию что после выполнении параллельного запроса,
// элемент будет выставлен в свою изначальную позицию в коллекции.

//зачем нужен WithDegreeOfParallelism().
// на сколько задач можно разделить наш запрос для его обработки. 