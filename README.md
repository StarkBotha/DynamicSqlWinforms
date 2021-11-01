# Dynamic Sql Winforms
Dynamically create Winforms UI from extended sql scripts

# .NET 4.8, SQL, Entity Framework 6

The idea here is to allow an operations team to perform maintenance on a production database through a UI which is dynamically generated via custom extended SQL scripts.

A sql script is extended at the top via a custom script written entirely in comments, the sql file is then placed in a dedicated folder, then when the app starts up it loads all of the scripts in that folder into a listbox which contains a light description. When the user then selects an item from that list box, the application generates a fillable form UI dynamically according to the described parameters in the custom comment script.

Why? This eliminates the need for operations staff to know SQL and more importantly protects them from making catastropic mistakes such as performing a query without a WHERE clause, etc.
