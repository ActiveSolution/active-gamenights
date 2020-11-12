module Backend.Domain.User

open Backend

type CreateUser = string -> Result<User, ValidationError>

let createUser : CreateUser = User.create 
