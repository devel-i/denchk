package ru.practice.books;

import android.app.*;
import android.os.Bundle;
import android.content.*;
import android.database.Cursor;
import android.database.sqlite.*;
import android.text.*;
import android.view.*;
import android.widget.*;

public class MainActivity extends Activity {
    private BookDb db;
    private LinearLayout root, list;
    private EditText search;
    private int selectedId = -1;
    private String selectedTitle = "", selectedAuthor = "";

    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        db = new BookDb(this);
        showLogin();
    }
    private void page(String title) {
        root = new LinearLayout(this); root.setOrientation(LinearLayout.VERTICAL); root.setPadding(18, 18, 18, 18);
        ScrollView scroll = new ScrollView(this); scroll.addView(root); setContentView(scroll);
        TextView heading = new TextView(this); heading.setText(title); heading.setTextSize(24); root.addView(heading);
    }
    private EditText input(String hint) {
        EditText field = new EditText(this); field.setHint(hint); field.setSingleLine(true); root.addView(field); return field;
    }
    private void button(String text, Runnable action) {
        Button b = new Button(this); b.setText(text); b.setOnClickListener(v -> action.run()); root.addView(b);
    }
    private void info(String message) { new AlertDialog.Builder(this).setTitle("Сообщение").setMessage(message).setPositiveButton("ОК", null).show(); }
    private void showLogin() {
        page("Книги — вход");
        EditText login = input("Логин");
        button("Войти", () -> {
            if (login.getText().toString().trim().equals("student")) showBooks();
            else info("Неверный логин. Для учебного входа используйте student.");
        });
    }
    private void showBooks() {
        page("Список книг");
        button("Выход", this::showLogin);
        search = input("Поиск по названию или автору");
        search.addTextChangedListener(new TextWatcher() {
            public void beforeTextChanged(CharSequence s, int start, int count, int after) {}
            public void onTextChanged(CharSequence s, int start, int before, int count) { refresh(); }
            public void afterTextChanged(Editable e) {}
        });
        button("Добавить книгу", () -> editDialog(false));
        button("Изменить выбранную", () -> { if (selectedId < 0) info("Сначала выберите книгу."); else editDialog(true); });
        button("Удалить выбранную", () -> {
            if (selectedId < 0) { info("Сначала выберите книгу."); return; }
            new AlertDialog.Builder(this).setTitle("Подтверждение").setMessage("Удалить книгу «" + selectedTitle + "»?")
                .setPositiveButton("Удалить", (d, w) -> { db.delete(selectedId); selectedId = -1; refresh(); })
                .setNegativeButton("Отмена", null).show();
        });
        list = new LinearLayout(this); list.setOrientation(LinearLayout.VERTICAL); root.addView(list); refresh();
    }
    private void refresh() {
        if (list == null || search == null) return;
        list.removeAllViews();
        int count = 0;
        try (Cursor c = db.find(search.getText().toString().trim())) {
            while (c.moveToNext()) {
                int id = c.getInt(0); String title = c.getString(1), author = c.getString(2);
                Button row = new Button(this); row.setAllCaps(false); row.setText(title + " — " + author + (id == selectedId ? "  ✓" : ""));
                row.setOnClickListener(v -> { selectedId = id; selectedTitle = title; selectedAuthor = author; refresh(); });
                list.addView(row); count++;
            }
        }
        TextView summary = new TextView(this); summary.setText(count == 0 ? "Ничего не найдено" : "Найдено: " + count); list.addView(summary);
    }
    private void editDialog(boolean editing) {
        LinearLayout form = new LinearLayout(this); form.setOrientation(LinearLayout.VERTICAL); form.setPadding(20, 0, 20, 0);
        EditText title = new EditText(this), author = new EditText(this);
        title.setHint("Название"); author.setHint("Автор");
        if (editing) { title.setText(selectedTitle); author.setText(selectedAuthor); }
        form.addView(title); form.addView(author);
        AlertDialog dialog = new AlertDialog.Builder(this).setTitle(editing ? "Изменить книгу" : "Новая книга").setView(form)
            .setPositiveButton("Сохранить", null).setNegativeButton("Отмена", null).create();
        dialog.setOnShowListener(x -> dialog.getButton(AlertDialog.BUTTON_POSITIVE).setOnClickListener(v -> {
            String t = title.getText().toString().trim(), a = author.getText().toString().trim();
            if (t.isEmpty() || a.isEmpty() || t.length() > 100 || a.length() > 100) { info("Заполните название и автора (не более 100 символов)."); return; }
            if (editing) db.update(selectedId, t, a); else db.add(t, a);
            selectedId = -1; dialog.dismiss(); refresh();
        }));
        dialog.show();
    }
    static class BookDb extends SQLiteOpenHelper {
        BookDb(Context context) { super(context, "books.db", null, 1); }
        @Override public void onCreate(SQLiteDatabase db) { db.execSQL("CREATE TABLE books (id INTEGER PRIMARY KEY AUTOINCREMENT, title TEXT NOT NULL, author TEXT NOT NULL)"); }
        @Override public void onUpgrade(SQLiteDatabase db, int oldVersion, int newVersion) {}
        void add(String title, String author) { ContentValues v = values(title, author); getWritableDatabase().insertOrThrow("books", null, v); }
        void update(int id, String title, String author) { getWritableDatabase().update("books", values(title, author), "id=?", new String[]{String.valueOf(id)}); }
        void delete(int id) { getWritableDatabase().delete("books", "id=?", new String[]{String.valueOf(id)}); }
        Cursor find(String query) { return getReadableDatabase().rawQuery("SELECT id,title,author FROM books WHERE title LIKE ? OR author LIKE ? ORDER BY title", new String[]{"%" + query + "%", "%" + query + "%"}); }
        private ContentValues values(String title, String author) { ContentValues v = new ContentValues(); v.put("title", title); v.put("author", author); return v; }
    }
}
