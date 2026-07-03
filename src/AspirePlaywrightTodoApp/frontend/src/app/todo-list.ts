import { Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

interface Todo {
  id: number;
  title: string;
  isDone: boolean;
  createdAt: string;
}

@Component({
  selector: 'app-todo-list',
  templateUrl: './todo-list.html',
  styleUrl: './todo-list.css',
})
export class TodoList implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly todos = signal<Todo[]>([]);
  protected readonly newTitle = signal('');

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.http.get<Todo[]>('/api/todos').subscribe((todos) => this.todos.set(todos));
  }

  protected add(): void {
    const title = this.newTitle().trim();
    if (!title) {
      return;
    }
    this.http.post<Todo>('/api/todos', { title }).subscribe(() => {
      this.newTitle.set('');
      this.load();
    });
  }

  protected toggle(todo: Todo): void {
    this.http
      .put(`/api/todos/${todo.id}`, { title: todo.title, isDone: !todo.isDone })
      .subscribe(() => this.load());
  }

  protected remove(todo: Todo): void {
    this.http.delete(`/api/todos/${todo.id}`).subscribe(() => this.load());
  }
}
